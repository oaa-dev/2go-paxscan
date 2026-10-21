// Talks to a running hub exactly like the app does (pinned TLS, real client).
// Usage: dart run tool/hub_smoke.dart '<pairing QR JSON>'
import 'dart:io';

import 'package:paxscan_core/paxscan_core.dart';
import 'package:uuid/uuid.dart';

Future<void> main(List<String> args) async {
  if (args.isEmpty) {
    stderr.writeln("usage: dart run tool/hub_smoke.dart '<pairing QR JSON>'");
    exit(64);
  }
  final payload = switch (PairingPayload.parse(args.first)) {
    Ok(:final value) => value,
    Err(:final failure) => throw StateError(failure.message),
  };

  final anonymous = DioHubApiClient(HubConnection(hubUrl: payload.hubUrl, certSha256: payload.certSha256));
  final paired = await anonymous.pair(
    pairingToken: payload.pairingToken,
    deviceName: 'smoke-test',
    platform: 'android',
    appVersion: '0.0.0',
  );
  stdout.writeln('paired: device ${paired.deviceId}');

  final client = DioHubApiClient(
    HubConnection(
      hubUrl: payload.hubUrl,
      certSha256: payload.certSha256,
      deviceId: paired.deviceId,
      deviceSecret: paired.deviceSecret,
    ),
  );
  final ping = await client.ping(pendingCount: 1, lastSeq: 0);
  stdout.writeln('ping: sync_requested=${ping.syncRequested}');

  final hello = await client.hello(lastSeq: 1, pendingCount: 1, deviceTime: DateTime.now());
  stdout.writeln('hello: expected_from_seq=${hello.expectedFromSeq}');

  final before = await client.snapshot();
  final target = before.tasks.firstOrNull;
  final update = StatusUpdateDto(
    id: const Uuid().v7(),
    taskId: target?.id ?? const Uuid().v4(),
    fromStatus: target?.status ?? TaskStatus.pending,
    toStatus: target?.status == TaskStatus.done ? TaskStatus.inProgress : TaskStatus.done,
    baseVersion: target?.version ?? 1,
    deviceSeq: 1,
    deviceCreatedAt: DateTime.now(),
  );
  for (final attempt in [1, 2]) {
    final push = await client.push([update]);
    final r = push.results.single;
    stdout.writeln('push #$attempt: ${r.result}${r.reason == null ? '' : ' (${r.reason})'} task_version=${r.taskVersion}');
  }

  final after = await client.snapshot();
  stdout.writeln('snapshot: ${after.tasks.length} tasks, dataset ${after.datasetVersion}');
  for (final t in after.tasks) {
    stdout.writeln('  - ${t.name} [${t.type.wire}] ${t.status.wire} v${t.version}');
  }

  final wrongPin = DioHubApiClient(HubConnection(hubUrl: payload.hubUrl, certSha256: '0' * 64));
  try {
    await wrongPin.ping(pendingCount: 0, lastSeq: 0);
    stdout.writeln('wrong pin: UNEXPECTEDLY ACCEPTED');
  } on HubApiException catch (e) {
    stdout.writeln('wrong pin: refused (${e.failure.kind.name})');
  }
}
