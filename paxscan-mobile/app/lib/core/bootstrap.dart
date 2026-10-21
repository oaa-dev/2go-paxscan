import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:path_provider/path_provider.dart';
import 'package:paxscan_core/paxscan_core.dart';

/// Opens the encrypted database, creating its key in the platform keystore on first run.
Future<AppDatabase> openAppDatabase(SecureStore secureStore) async {
  var key = await secureStore.read(SecureKeys.databaseKey);
  if (key == null) {
    final random = Random.secure();
    key = base64UrlEncode(List<int>.generate(32, (_) => random.nextInt(256)));
    await secureStore.write(SecureKeys.databaseKey, key);
  }

  final directory = await getApplicationSupportDirectory();
  return openEncryptedDatabase(File('${directory.path}/paxscan.db'), key);
}
