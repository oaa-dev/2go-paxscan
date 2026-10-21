import 'package:drift/drift.dart';
import 'package:paxscan_core/src/data/db/app_database.dart';
import 'package:paxscan_core/src/data/hub/hub_api_client.dart';
import 'package:paxscan_core/src/data/secure/secure_store.dart';
import 'package:paxscan_core/src/domain/result.dart';
import 'package:paxscan_core/src/pairing/pairing_payload.dart';
import 'package:paxscan_core/src/repositories/meta_keys.dart';

/// What the device knows about the hub it is paired with.
final class PairedHub {
  const new({
    required this.deviceId,
    required this.deviceName,
    required this.hubId,
    required this.hubUrl,
    required this.certSha256,
  });

  final String deviceId;
  final String deviceName;
  final String hubId;
  final Uri hubUrl;
  final String certSha256;
}

class PairingRepository {
  new({required this._db, required this._secureStore, required this._clientFactory});

  final AppDatabase _db;
  final SecureStore _secureStore;
  final HubApiClientFactory _clientFactory;

  Future<PairedHub?> currentHub() async {
    final values = {for (final key in MetaKeys.pairing) key: await _db.readMeta(key)};
    if (values.values.any((v) => v == null)) {
      return null;
    }
    return PairedHub(
      deviceId: values[MetaKeys.deviceId]!,
      deviceName: values[MetaKeys.deviceName]!,
      hubId: values[MetaKeys.hubId]!,
      hubUrl: Uri.parse(values[MetaKeys.hubUrl]!),
      certSha256: values[MetaKeys.certSha256]!,
    );
  }

  /// Connection with credentials for authenticated hub calls, or null when not paired.
  Future<HubConnection?> connection() async {
    final hub = await currentHub();
    final secret = await _secureStore.read(SecureKeys.deviceSecret);
    if (hub == null || secret == null) {
      return null;
    }
    return HubConnection(hubUrl: hub.hubUrl, certSha256: hub.certSha256, deviceId: hub.deviceId, deviceSecret: secret);
  }

  Future<Result<PairedHub>> pair({
    required PairingPayload payload,
    required String deviceName,
    required String platform,
    required String appVersion,
  }) async {
    final client = _clientFactory(HubConnection(hubUrl: payload.hubUrl, certSha256: payload.certSha256));
    try {
      final response = await client.pair(
        pairingToken: payload.pairingToken,
        deviceName: deviceName,
        platform: platform,
        appVersion: appVersion,
      );

      await _secureStore.write(SecureKeys.deviceSecret, response.deviceSecret);
      await _db.transaction(() async {
        await _db.writeMeta(MetaKeys.deviceId, response.deviceId);
        await _db.writeMeta(MetaKeys.deviceName, deviceName);
        await _db.writeMeta(MetaKeys.hubId, response.hubId);
        await _db.writeMeta(MetaKeys.hubUrl, payload.hubUrl.toString());
        await _db.writeMeta(MetaKeys.certSha256, payload.certSha256);
      });
      return Ok((await currentHub())!);
    } on HubApiException catch (e) {
      return Err(e.failure);
    }
  }

  /// Forgets the hub and its data. Refused while updates have not reached the hub.
  Future<Result<void>> unpair() async {
    final unsent = await (_db.selectOnly(_db.outbox)
          ..addColumns([_db.outbox.id.count()])
          ..where(_db.outbox.state.isNotValue(OutboxState.acked.name)))
        .map((row) => row.read(_db.outbox.id.count()) ?? 0)
        .getSingle();
    if (unsent > 0) {
      return Err(Failure(FailureKind.blocked, '$unsent updates have not been synced yet. Sync first.'));
    }

    await _db.transaction(() async {
      await _db.delete(_db.tasks).go();
      await _db.delete(_db.outbox).go();
      await _db.deleteMeta([...MetaKeys.pairing, MetaKeys.datasetVersion, MetaKeys.lastSyncAt]);
    });
    await _secureStore.delete(SecureKeys.deviceSecret);
    return const Ok(null);
  }
}
