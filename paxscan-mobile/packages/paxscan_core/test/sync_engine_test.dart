import 'package:paxscan_core/paxscan_core.dart';
import 'package:test/test.dart';

import 'fakes.dart';

void main() {
  late TestApp app;

  setUp(() async {
    app = await TestApp.create();
    await app.receive([taskDto('t1'), taskDto('t2', name: 'Patrol deck 5')]);
  });

  tearDown(() => app.db.close());

  test('sync runs hello, push, snapshot in that order and acks every answered item', () async {
    await app.tasks.changeStatus('t1', TaskStatus.done);
    app.hub.tasks = [taskDto('t1', status: TaskStatus.done, version: 2), taskDto('t2')];

    final result = await app.sync.sync();

    expect(app.hub.calls, ['hello', 'push', 'snapshot']);
    expect((result as Ok<SyncReport>).value.pushed, 1);
    final entry = (await app.outbox()).single;
    expect(entry.state, OutboxState.acked.name);
    expect(entry.result, 'accepted');
  });

  test('a lost answer is resent and the hub reply already_received acks it', () async {
    await app.tasks.changeStatus('t1', TaskStatus.done);
    app.hub.failNextPush = const HubApiException(Failure(FailureKind.unreachable, 'timeout'));

    final first = await app.sync.sync();
    expect(first, isA<Err<SyncReport>>());
    var entry = (await app.outbox()).single;
    expect(entry.state, OutboxState.saved.name, reason: 'never acked without an explicit result');
    expect(entry.attempts, 1);

    await app.sync.sync();
    entry = (await app.outbox()).single;
    expect(entry.state, OutboxState.acked.name);
    expect(entry.result, 'already_received');
    expect(app.hub.pushes.last.single.id, entry.id, reason: 'same id resent, no duplicate created');
  });

  test('an item without a result stays queued', () async {
    final saved = await app.tasks.changeStatus('t1', TaskStatus.done) as Ok<OutboxEntry>;
    app.hub.dropResultsFor = {saved.value.id};

    await app.sync.sync();

    final entry = (await app.outbox()).single;
    expect(entry.state, OutboxState.saved.name);
    expect(entry.lastError, 'No result from hub');
  });

  test('snapshot keeps local changes the hub has not stored yet', () async {
    final saved = await app.tasks.changeStatus('t1', TaskStatus.done) as Ok<OutboxEntry>;
    app.hub
      ..dropResultsFor = {saved.value.id}
      ..tasks = [taskDto('t1', name: 'Renamed'), taskDto('t2')];

    await app.sync.sync();

    final task = await app.task('t1');
    expect(task.name, 'Renamed');
    expect(task.status, 'done');
  });

  test('snapshot removes tasks deleted on the hub', () async {
    app.hub.tasks = [taskDto('t2')];

    await app.sync.sync();

    expect(await app.db.select(app.db.tasks).get(), hasLength(1));
  });

  test('concurrent sync calls share one run', () async {
    await app.tasks.changeStatus('t1', TaskStatus.done);

    await Future.wait([app.sync.sync(), app.sync.sync()]);

    expect(app.hub.calls.where((c) => c == 'hello'), hasLength(1));
  });

  test('pushes in batches of at most 200, in sequence order', () async {
    for (var i = 0; i < 250; i++) {
      await app.tasks.changeStatus('t1', i.isEven ? TaskStatus.done : TaskStatus.pending);
    }

    await app.sync.sync();

    expect(app.hub.pushes.map((p) => p.length), [200, 50]);
    final seqs = app.hub.pushes.expand((p) => p).map((i) => i.deviceSeq).toList();
    expect(seqs, List.generate(250, (i) => i + 1));
  });

  test('unpaired device cannot sync', () async {
    final unpaired = await TestApp.create(paired: false);
    addTearDown(unpaired.db.close);

    final result = await unpaired.sync.sync();

    expect((result as Err<SyncReport>).failure.kind, FailureKind.blocked);
  });

  test('heartbeat runs a sync when the desktop requested one', () async {
    await app.tasks.changeStatus('t1', TaskStatus.done);
    app.hub.syncRequested = true;
    final heartbeat = Heartbeat(db: app.db, pairing: app.pairing, clientFactory: (_) => app.hub, syncEngine: app.sync);
    addTearDown(heartbeat.dispose);

    final started = await heartbeat.beat();
    await app.sync.sync(); // joins the run the heartbeat started

    expect(started, isTrue);
    expect(heartbeat.current, HubReachability.reachable);
    expect((await app.outbox()).single.state, OutboxState.acked.name);
  });
}
