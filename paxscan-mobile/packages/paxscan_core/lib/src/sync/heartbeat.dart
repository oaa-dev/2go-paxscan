import 'dart:async';

import 'package:drift/drift.dart';
import 'package:paxscan_core/src/data/db/app_database.dart';
import 'package:paxscan_core/src/data/hub/hub_api_client.dart';
import 'package:paxscan_core/src/domain/result.dart';
import 'package:paxscan_core/src/repositories/meta_keys.dart';
import 'package:paxscan_core/src/repositories/pairing_repository.dart';
import 'package:paxscan_core/src/sync/sync_engine.dart';

enum HubReachability { unknown, reachable, unreachable, unauthorized }

/// Pings the hub every [interval] while the app is in the foreground so the desktop can
/// show the device as online, and runs a sync when the desktop operator requests one.
class Heartbeat {
  new({
    required this._db,
    required this._pairing,
    required this._clientFactory,
    required this._syncEngine,
    this.interval = const Duration(seconds: 30),
  });

  final AppDatabase _db;
  final PairingRepository _pairing;
  final HubApiClientFactory _clientFactory;
  final SyncEngine _syncEngine;
  final Duration interval;

  final _reachability = StreamController<HubReachability>.broadcast();
  Timer? _timer;
  HubReachability _last = HubReachability.unknown;

  Stream<HubReachability> get reachability => _reachability.stream;

  HubReachability get current => _last;

  void start() {
    if (_timer != null) {
      return;
    }
    unawaited(beat());
    _timer = Timer.periodic(interval, (_) => unawaited(beat()));
  }

  void stop() {
    _timer?.cancel();
    _timer = null;
  }

  Future<void> dispose() async {
    stop();
    await _reachability.close();
  }

  /// One heartbeat. Returns true when a requested sync was started.
  Future<bool> beat() async {
    final connection = await _pairing.connection();
    if (connection == null) {
      return false;
    }

    try {
      final pending = await _pendingCount();
      final lastSeq = int.parse(await _db.readMeta(MetaKeys.lastSeq) ?? '0');
      final response = await _clientFactory(connection).ping(pendingCount: pending, lastSeq: lastSeq);
      _emit(HubReachability.reachable);
      if (response.syncRequested && !_syncEngine.isRunning) {
        unawaited(_syncEngine.sync());
        return true;
      }
    } on HubApiException catch (e) {
      _emit(e.failure.kind == FailureKind.unauthorized ? HubReachability.unauthorized : HubReachability.unreachable);
    }
    return false;
  }

  Future<int> _pendingCount() {
    final count = _db.outbox.id.count();
    return (_db.selectOnly(_db.outbox)
          ..addColumns([count])
          ..where(_db.outbox.state.isNotValue(OutboxState.acked.name)))
        .map((row) => row.read(count) ?? 0)
        .getSingle();
  }

  void _emit(HubReachability value) {
    _last = value;
    if (!_reachability.isClosed) {
      _reachability.add(value);
    }
  }
}
