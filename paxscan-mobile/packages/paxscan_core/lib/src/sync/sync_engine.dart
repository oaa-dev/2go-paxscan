import 'dart:async';

import 'package:drift/drift.dart';
import 'package:paxscan_core/src/data/db/app_database.dart';
import 'package:paxscan_core/src/data/hub/hub_api_client.dart';
import 'package:paxscan_core/src/data/hub/hub_dto.dart';
import 'package:paxscan_core/src/domain/result.dart';
import 'package:paxscan_core/src/domain/task_enums.dart';
import 'package:paxscan_core/src/repositories/meta_keys.dart';
import 'package:paxscan_core/src/repositories/pairing_repository.dart';

final class SyncReport {
  const new({
    required this.pushed,
    required this.conflicts,
    required this.rejected,
    required this.tasksReceived,
    required this.seqGapDetected,
    required this.finishedAt,
  });

  final int pushed;
  final int conflicts;
  final int rejected;
  final int tasksReceived;
  final bool seqGapDetected;
  final DateTime finishedAt;
}

/// Device ↔ hub sync: hello → push (outbox, seq order, ≤ 200 per batch) → snapshot.
///
/// An outbox item is marked acked only on an explicit per-item result from the hub;
/// anything else (timeout, crash, missing result) leaves it to be sent again, which
/// is safe because the hub is idempotent by update id.
class SyncEngine {
  new({
    required this._db,
    required this._pairing,
    required this._clientFactory,
    DateTime Function()? clock,
  }) : _clock = clock ?? DateTime.now;

  static const batchSize = 200;

  final AppDatabase _db;
  final PairingRepository _pairing;
  final HubApiClientFactory _clientFactory;
  final DateTime Function() _clock;
  Future<Result<SyncReport>>? _running;

  bool get isRunning => _running != null;

  /// Runs a sync; concurrent callers share the one already running.
  Future<Result<SyncReport>> sync() => _running ??= _sync().whenComplete(() => _running = null);

  Future<Result<SyncReport>> _sync() async {
    final connection = await _pairing.connection();
    if (connection == null) {
      return const Err(Failure(FailureKind.blocked, 'This device is not paired with a hub.'));
    }
    final client = _clientFactory(connection);

    try {
      final pending = await _unsent();
      final lastSeq = int.parse(await _db.readMeta(MetaKeys.lastSeq) ?? '0');
      await client.hello(lastSeq: lastSeq, pendingCount: pending.length, deviceTime: _clock());

      var pushed = 0;
      var conflicts = 0;
      var rejected = 0;
      var gap = false;
      for (var i = 0; i < pending.length; i += batchSize) {
        final batch = pending.sublist(i, i + batchSize > pending.length ? pending.length : i + batchSize);
        final response = await _pushBatch(client, batch);
        pushed += response.results.length;
        conflicts += response.results.where((r) => r.result == 'conflict').length;
        rejected += response.results.where((r) => r.result == 'rejected').length;
        gap |= response.seqGapDetected;
      }

      final snapshot = await client.snapshot();
      await _applySnapshot(snapshot);

      final finishedAt = _clock().toUtc();
      await _db.writeMeta(MetaKeys.lastSyncAt, finishedAt.toIso8601String());
      return Ok(
        SyncReport(
          pushed: pushed,
          conflicts: conflicts,
          rejected: rejected,
          tasksReceived: snapshot.tasks.length,
          seqGapDetected: gap,
          finishedAt: finishedAt,
        ),
      );
    } on HubApiException catch (e) {
      await _releaseSending(e.failure.message);
      return Err(e.failure);
    }
  }

  Future<List<OutboxEntry>> _unsent() => (_db.select(_db.outbox)
        ..where((o) => o.state.isNotValue(OutboxState.acked.name))
        ..orderBy([(o) => OrderingTerm(expression: o.deviceSeq)]))
      .get();

  Future<PushResponse> _pushBatch(HubApiClient client, List<OutboxEntry> batch) async {
    final ids = batch.map((e) => e.id).toList();
    await (_db.update(_db.outbox)..where((o) => o.id.isIn(ids))).write(
      OutboxCompanion(state: Value(OutboxState.sending.name)),
    );

    final response = await client.push([
      for (final e in batch)
        StatusUpdateDto(
          id: e.id,
          taskId: e.taskId,
          fromStatus: _status(e.fromStatus),
          toStatus: _status(e.toStatus),
          baseVersion: e.baseVersion,
          deviceSeq: e.deviceSeq,
          deviceCreatedAt: e.deviceCreatedAt,
        ),
    ]);

    final now = _clock().toUtc();
    final results = {for (final r in response.results) r.id: r};
    await _db.transaction(() async {
      for (final e in batch) {
        final result = results[e.id];
        await (_db.update(_db.outbox)..where((o) => o.id.equals(e.id))).write(
          result == null
              ? OutboxCompanion(
                  state: Value(OutboxState.saved.name),
                  attempts: Value(e.attempts + 1),
                  lastError: const Value('No result from hub'),
                )
              : OutboxCompanion(
                  state: Value(OutboxState.acked.name),
                  attempts: Value(e.attempts + 1),
                  result: Value(result.result),
                  resultReason: Value(result.reason),
                  ackedAt: Value(now),
                  lastError: const Value(null),
                ),
        );
      }
    });
    return response;
  }

  /// Replaces local tasks with the hub's, then re-applies changes the hub has not stored yet.
  Future<void> _applySnapshot(SnapshotResponse snapshot) => _db.transaction(() async {
    await _db.delete(_db.tasks).go();
    await _db.batch((b) {
      b.insertAll(_db.tasks, [
        for (final t in snapshot.tasks)
          TasksCompanion.insert(
            id: t.id,
            name: t.name,
            type: t.type.wire,
            description: Value(t.description),
            status: t.status.wire,
            version: t.version,
            updatedAt: t.updatedAt,
          ),
      ]);
    });

    for (final e in await _unsent()) {
      await (_db.update(_db.tasks)..where((t) => t.id.equals(e.taskId))).write(
        TasksCompanion(status: Value(e.toStatus)),
      );
    }

    if (snapshot.datasetVersion case final version?) {
      await _db.writeMeta(MetaKeys.datasetVersion, version);
    }
  });

  Future<void> _releaseSending(String error) async {
    final sending = await (_db.select(_db.outbox)..where((o) => o.state.equals(OutboxState.sending.name))).get();
    await _db.transaction(() async {
      for (final e in sending) {
        await (_db.update(_db.outbox)..where((o) => o.id.equals(e.id))).write(
          OutboxCompanion(state: Value(OutboxState.saved.name), attempts: Value(e.attempts + 1), lastError: Value(error)),
        );
      }
    });
  }

  static TaskStatus _status(String wire) => TaskStatus.fromWire(wire);
}
