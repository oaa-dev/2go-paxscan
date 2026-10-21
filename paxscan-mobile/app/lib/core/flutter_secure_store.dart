import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:paxscan_core/paxscan_core.dart';

/// Android Keystore / iOS Keychain. Items stay on this device (not in backups/iCloud).
final class FlutterSecureStore implements SecureStore {
  const new([this._storage = const FlutterSecureStorage(
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock_this_device),
  )]);

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read(String key) => _storage.read(key: key);

  @override
  Future<void> write(String key, String value) => _storage.write(key: key, value: value);

  @override
  Future<void> delete(String key) => _storage.delete(key: key);
}
