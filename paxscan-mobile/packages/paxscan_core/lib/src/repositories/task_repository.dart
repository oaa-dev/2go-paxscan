import 'package:drift/drift.dart';
import 'package:paxscan_core/src/data/db/app_database.dart';
import 'package:paxscan_core/src/domain/result.dart';
import 'package:paxscan_core/src/domain/task_enums.dart';
import 'package:paxscan_core/src/repositories/meta_keys.dart';
import 'package:uuid/uuid.dart';

class TaskRepository {
  new(this._db, {DateTime Function()? clock, Uuid? uuid})
    : _clock = clock ?? DateTime.now,
      _uuid = uuid ?? const Uuid();

  final AppDatabase _db;
  final DateTime Function() _clock;
  final Uuid _uuid;

  Stream<List<TaskRow>> watchTasks({TaskType? type, TaskStatus? status}) {
    final query = _db.select(_db.tasks)
      ..where((t) => type == null ? const Constant(true) : t.type.equals(type.wire))
      ..where((t) => status == null ? const Constant(true) : t.status.equals(status.wire))
      ..orderBy([(t) => OrderingTerm(expression: t.type), (t) => OrderingTerm(expression: t.name.lower())]);
    return query.watch();
  }

  Stream<TaskRow?> watchTask(String id) =>
      (_db.select(_db.tasks)..where((t) => t.id.equals(id))).watchSingleOrNull();

  /// Updates not yet confirmed by the hub.
  Stream<int> watchUnsyncedCount() {
    final count = _db.outbox.id.count();
    return (_db.selectOnly(_db.outbox)
          ..addColumns([count])
          ..where(_db.outbox.state.isNotValue(OutboxState.acked.name)))
        .map((row) => row.read(count) ?? 0)
        .watchSingle();
  }

  /// Saves a status change locally (works offline). One transaction: next device_seq,
  /// outbox row and optimistic task update — so a crash can never lose half of it.
  Future<Result<OutboxEntry>> changeStatus(String taskId, TaskStatus to) => _db.transaction(() async {
    final task = await (_db.select(_db.tasks)..where((t) => t.id.equals(taskId))).getSingleOrNull();
    if (task == null) {
      return const Err(Failure(FailureKind.invalid, 'Task not found.'));
    }
    final from = TaskStatus.fromWire(task.status);
    if (from == to) {
      return Err(Failure(FailureKind.invalid, 'Task is already ${to.label.toLowerCase()}.'));
    }

    final seq = int.parse(await _db.readMeta(MetaKeys.lastSeq) ?? '0') + 1;
    final now = _clock().toUtc();
    final entry = OutboxCompanion.insert(
      id: _uuid.v7(),
      taskId: taskId,
      fromStatus: from.wire,
      toStatus: to.wire,
      baseVersion: task.version,
      deviceSeq: seq,
      deviceCreatedAt: now,
      state: OutboxState.saved.name,
    );

    await _db.writeMeta(MetaKeys.lastSeq, '$seq');
    final saved = await _db.into(_db.outbox).insertReturning(entry);
    await (_db.update(_db.tasks)..where((t) => t.id.equals(taskId))).write(
      TasksCompanion(status: Value(to.wire), version: Value(task.version + 1), updatedAt: Value(now)),
    );
    return Ok(saved);
  });

  Future<List<OutboxEntry>> recentUpdates({int limit = 50}) => (_db.select(_db.outbox)
        ..orderBy([(o) => OrderingTerm.desc(o.deviceSeq)])
        ..limit(limit))
      .get();
}
