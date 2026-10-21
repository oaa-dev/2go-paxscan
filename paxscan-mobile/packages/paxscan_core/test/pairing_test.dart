import 'dart:convert';

import 'package:paxscan_core/paxscan_core.dart';
import 'package:test/test.dart';

import 'fakes.dart';

void main() {
  final fingerprint = 'b5e8' * 16;

  String qr({String url = 'https://192.168.137.1:5443', String? cert, int v = 1}) => jsonEncode({
    'v': v,
    'hub_url': url,
    'cert_sha256': cert ?? fingerprint,
    'pairing_token': 'tok',
    'hub_id': 'hub-1',
  });

  group('PairingPayload.parse', () {
    test('accepts a hub QR code', () {
      final result = PairingPayload.parse(qr()) as Ok<PairingPayload>;

      expect(result.value.hubUrl.port, 5443);
      expect(result.value.certSha256, fingerprint);
    });

    test('rejects plain http, bad fingerprints, other versions and random text', () {
      expect(PairingPayload.parse(qr(url: 'http://192.168.137.1:5443')), isA<Err<PairingPayload>>());
      expect(PairingPayload.parse(qr(cert: 'abc')), isA<Err<PairingPayload>>());
      expect(PairingPayload.parse(qr(v: 2)), isA<Err<PairingPayload>>());
      expect(PairingPayload.parse('hello'), isA<Err<PairingPayload>>());
    });
  });

  group('PairingRepository', () {
    test('stores the hub and keeps the secret in secure storage only', () async {
      final app = await TestApp.create();
      addTearDown(app.db.close);

      final hub = await app.pairing.currentHub();
      expect(hub!.deviceId, 'device-1');
      expect(await app.secureStore.read(SecureKeys.deviceSecret), 'secret-1');
      final metaValues = (await app.db.select(app.db.meta).get()).map((m) => m.value);
      expect(metaValues, isNot(contains('secret-1')));
    });

    test('an expired pairing code fails without storing anything', () async {
      final app = await TestApp.create(paired: false);
      addTearDown(app.db.close);
      final payload = (PairingPayload.parse(qr()) as Ok<PairingPayload>).value;

      final result = await app.pairing.pair(
        payload: PairingPayload(
          hubUrl: payload.hubUrl,
          certSha256: payload.certSha256,
          pairingToken: 'expired',
          hubId: payload.hubId,
        ),
        deviceName: 'Chef 1',
        platform: 'android',
        appVersion: '1.0.0',
      );

      expect(result, isA<Err<PairedHub>>());
      expect(await app.pairing.currentHub(), isNull);
    });

    test('unpair is refused while updates are unsynced', () async {
      final app = await TestApp.create();
      addTearDown(app.db.close);
      await app.receive([taskDto('t1')]);
      app.hub.dropResultsFor = {};
      await app.tasks.changeStatus('t1', TaskStatus.done);

      final blocked = await app.pairing.unpair();
      expect((blocked as Err<void>).failure.kind, FailureKind.blocked);

      await app.sync.sync();
      expect(await app.pairing.unpair(), isA<Ok<void>>());
      expect(await app.pairing.currentHub(), isNull);
      expect(await app.secureStore.read(SecureKeys.deviceSecret), isNull);
    });
  });
}
