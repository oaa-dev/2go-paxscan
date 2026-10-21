abstract final class MetaKeys {
  static const deviceId = 'device_id';
  static const deviceName = 'device_name';
  static const hubId = 'hub_id';
  static const hubUrl = 'hub_url';
  static const certSha256 = 'cert_sha256';
  static const lastSeq = 'last_seq';
  static const datasetVersion = 'dataset_version';
  static const lastSyncAt = 'last_sync_at';

  static const List<String> pairing = [deviceId, deviceName, hubId, hubUrl, certSha256];
}
