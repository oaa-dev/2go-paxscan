import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:paxscan_core/paxscan_core.dart';

/// Scriptable hub for sync tests.
final class FakeHub implements HubApiClient {
  final List<String> calls = [];
  final List<List<StatusUpdateDto>> pushes = [];

  /// Hub-side record of stored update ids (to answer already_received).
  final Set<String> stored = {};
  List<TaskDto> tasks = [];
  bool syncRequested = false;

  /// When set, the next push throws this and is then cleared.
  HubApiException? failNextPush;

  /// Ids the hub "forgets" to answer.
  Set<String> dropResultsFor = {};

  String Function(StatusUpdateDto item) resultFor = (_) => 'accepted';

  @override
  Future<PairResponse> pair({
    required String pairingToken,
    required String deviceName,
    required String platform,
    required String appVersion,
  }) async {
    calls.add('pair');
    if (pairingToken == 'expired') {
      throw const HubApiException(Failure(FailureKind.unauthorized, 'Pairing code is invalid or expired.'));
    }
    return const PairResponse(deviceId: 'device-1', deviceSecret: 'secret-1', hubId: 'hub-1');
  }

  @override
  Future<PingResponse> ping({required int pendingCount, required int lastSeq}) async {
    calls.add('ping');
    return PingResponse(syncRequested: syncRequested, serverTime: DateTime.utc(2026, 10, 15));
  }

  @override
  Future<HelloResponse> hello({required int lastSeq, required int pendingCount, required DateTime deviceTime}) async {
    calls.add('hello');
    return HelloResponse(expectedFromSeq: 1, serverTime: DateTime.utc(2026, 10, 15));
  }

  @override
  Future<PushResponse> push(List<StatusUpdateDto> items) async {
    calls.add('push');
    pushes.add(items);
    if (failNextPush case final failure?) {
      failNextPush = null;
      stored.addAll(items.map((i) => i.id)); // Hub stored them, but the answer never arrived.
      throw failure;
    }
    final results = <ItemResultDto>[];
    for (final item in items) {
      final result = stored.contains(item.id) ? 'already_received' : resultFor(item);
      stored.add(item.id);
      if (!dropResultsFor.contains(item.id)) {
        results.add(ItemResultDto(id: item.id, result: result, taskVersion: item.baseVersion + 1));
      }
    }
    return PushResponse(results: results, seqGapDetected: false);
  }

  @override
  Future<SnapshotResponse> snapshot() async {
    calls.add('snapshot');
    return SnapshotResponse(datasetVersion: 'v1', tasks: tasks);
  }
}

TaskDto taskDto(String id, {TaskStatus status = TaskStatus.pending, int version = 1, String name = 'Prep breakfast'}) =>
    TaskDto(
      id: id,
      name: name,
      type: TaskType.chef,
      description: null,
      status: status,
      version: version,
      updatedAt: DateTime.utc(2026, 10, 15),
    );

/// In-memory database plus repositories wired to [hub], already paired.
final class TestApp {
  new _(this.db, this.hub, this.secureStore) {
    pairing = PairingRepository(db: db, secureStore: secureStore, clientFactory: (_) => hub);
    tasks = TaskRepository(db, clock: () => DateTime.utc(2026, 10, 15, 9));
    sync = SyncEngine(db: db, pairing: pairing, clientFactory: (_) => hub);
  }

  final AppDatabase db;
  final FakeHub hub;
  final MemorySecureStore secureStore;
  late final PairingRepository pairing;
  late final TaskRepository tasks;
  late final SyncEngine sync;

  static Future<TestApp> create({bool paired = true, QueryExecutor? executor}) async {
    // Each test opens its own in-memory database on purpose.
    driftRuntimeOptions.dontWarnAboutMultipleDatabases = true;
    final app = TestApp._(AppDatabase(executor ?? NativeDatabase.memory()), FakeHub(), MemorySecureStore());
    if (paired) {
      final payload = PairingPayload(
        hubUrl: Uri.parse('https://192.168.137.1:5443'),
        certSha256: 'a' * 64,
        pairingToken: 'token',
        hubId: 'hub-1',
      );
      await app.pairing.pair(payload: payload, deviceName: 'Chef 1', platform: 'android', appVersion: '1.0.0');
    }
    return app;
  }

  /// Puts tasks on the device as if a sync had delivered them.
  Future<void> receive(List<TaskDto> tasks) async {
    hub.tasks = tasks;
    await sync.sync();
    hub.calls.clear();
    hub.pushes.clear();
  }

  Future<List<OutboxEntry>> outbox() => db.select(db.outbox).get();

  Future<TaskRow> task(String id) => (db.select(db.tasks)..where((t) => t.id.equals(id))).getSingle();
}
