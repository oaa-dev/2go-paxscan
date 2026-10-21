import 'dart:convert';
import 'dart:io';

import 'package:paxscan_core/paxscan_core.dart';
import 'package:test/test.dart';

void main() {
  late HttpServer server;
  late String hubFingerprint;
  late String otherFingerprint;

  String fingerprintOf(String pemPath) {
    // DER of the PEM certificate, hashed the same way the app does.
    final pem = File(pemPath).readAsStringSync();
    final body = pem.split('\n').where((l) => l.isNotEmpty && !l.startsWith('-----')).join();
    return sha256FingerprintOfDer(base64Decode(body));
  }

  setUpAll(() async {
    final context = SecurityContext()
      ..useCertificateChain('test/fixtures/hub.cert.pem')
      ..usePrivateKey('test/fixtures/hub.key.pem');
    server = await HttpServer.bindSecure(InternetAddress.loopbackIPv4, 0, context)
      ..listen((request) async {
        final response = request.response
          ..headers.contentType = ContentType.json
          ..write('{"sync_requested": true, "server_time": "2026-10-15T08:00:00Z"}');
        await response.close();
      });
    hubFingerprint = fingerprintOf('test/fixtures/hub.cert.pem');
    otherFingerprint = fingerprintOf('test/fixtures/other.cert.pem');
  });

  tearDownAll(() => server.close(force: true));

  HubApiClient client(String fingerprint) => DioHubApiClient(
    HubConnection(hubUrl: Uri.parse('https://127.0.0.1:${server.port}'), certSha256: fingerprint),
  );

  test('talks to the hub whose certificate matches the pinned fingerprint', () async {
    final response = await client(hubFingerprint).ping(pendingCount: 0, lastSeq: 0);

    expect(response.syncRequested, isTrue);
  });

  test('refuses a hub presenting a different certificate', () async {
    await expectLater(
      client(otherFingerprint).ping(pendingCount: 0, lastSeq: 0),
      throwsA(isA<HubApiException>().having((e) => e.failure.kind, 'kind', FailureKind.untrustedHub)),
    );
  });

  test('an unreachable hub is reported as unreachable', () async {
    final closed = DioHubApiClient(
      HubConnection(hubUrl: Uri.parse('https://127.0.0.1:1'), certSha256: hubFingerprint),
    );

    await expectLater(
      closed.ping(pendingCount: 0, lastSeq: 0),
      throwsA(isA<HubApiException>().having((e) => e.failure.kind, 'kind', FailureKind.unreachable)),
    );
  });
}
