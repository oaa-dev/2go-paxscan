import 'package:paxscan_core/paxscan_core.dart';
import 'package:test/test.dart';

import 'fakes.dart';

void main() {
  late TestApp app;

  setUp(() async {
    app = await TestApp.create();
    await app.receive([taskDto('t1', version: 3)]);
  });

  tearDown(() => app.db.close());

  test('a status change writes the outbox row and the task in one go', () async {
    final result = await app.tasks.changeStatus('t1', TaskStatus.inProgress);

    expect(result, isA<Ok<OutboxEntry>>());
    final entry = (await app.outbox()).single;
    expect(entry.deviceSeq, 1);
    expect(entry.baseVersion, 3);
    expect(entry.fromStatus, 'pending');
    expect(entry.toStatus, 'in_progress');
    expect(entry.state, OutboxState.saved.name);

    final task = await app.task('t1');
    expect(task.status, 'in_progress');
    expect(task.version, 4, reason: 'next change on this device builds on this one');
  });

  test('consecutive changes get consecutive sequence numbers and chained versions', () async {
    await app.tasks.changeStatus('t1', TaskStatus.inProgress);
    await app.tasks.changeStatus('t1', TaskStatus.done);

    final entries = (await app.outbox())..sort((a, b) => a.deviceSeq.compareTo(b.deviceSeq));
    expect(entries.map((e) => e.deviceSeq), [1, 2]);
    expect(entries.map((e) => e.baseVersion), [3, 4]);
    expect(entries.last.fromStatus, 'in_progress');
  });

  test('changing to the current status is refused and records nothing', () async {
    final result = await app.tasks.changeStatus('t1', TaskStatus.pending);

    expect(result, isA<Err<OutboxEntry>>());
    expect(await app.outbox(), isEmpty);
  });

  test('unsynced count follows the outbox', () async {
    await app.tasks.changeStatus('t1', TaskStatus.done);
    expect(await app.tasks.watchUnsyncedCount().first, 1);

    await app.sync.sync();
    expect(await app.tasks.watchUnsyncedCount().first, 0);
  });
}
