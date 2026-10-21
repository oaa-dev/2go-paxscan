/// Platform keystore (Android Keystore / iOS Keychain) behind an interface so the
/// core stays Flutter-free and testable.
abstract interface class SecureStore {
  Future<String?> read(String key);

  Future<void> write(String key, String value);

  Future<void> delete(String key);
}

abstract final class SecureKeys {
  static const deviceSecret = 'device_secret';
  static const databaseKey = 'database_key';
}

/// In-memory store for tests.
final class MemorySecureStore implements SecureStore {
  final Map<String, String> values = {};

  @override
  Future<String?> read(String key) async => values[key];

  @override
  Future<void> write(String key, String value) async => values[key] = value;

  @override
  Future<void> delete(String key) async => values.remove(key);
}
