import 'dart:convert';
import 'dart:io';

import 'package:paxscan_core/paxscan_core.dart';
import 'package:sqlite3/sqlite3.dart';
import 'package:test/test.dart';

void main() {
  late Directory dir;
  late File file;

  setUp(() {
    dir = Directory.systemTemp.createTempSync('paxscan_db_');
    file = File('${dir.path}/paxscan.db');
  });

  tearDown(() => dir.deleteSync(recursive: true));

  test('data survives closing and reopening (crash / app restart)', () async {
    var db = openEncryptedDatabase(file, 'k3y');
    await db.writeMeta('last_seq', '7');
    await db.close();

    db = openEncryptedDatabase(file, 'k3y');
    expect(await db.readMeta('last_seq'), '7');
    await db.close();
  });

  test('the file cannot be read without the key', () async {
    final db = openEncryptedDatabase(file, 'k3y');
    await db.writeMeta('device_id', 'secret-device');
    await db.close();

    expect(file.readAsStringSync(encoding: latin1), isNot(contains('secret-device')));

    final raw = sqlite3.open(file.path);
    addTearDown(raw.close);
    expect(() => raw.select('SELECT * FROM meta'), throwsA(isA<SqliteException>()));
  });

  test('the wrong key is refused', () async {
    final db = openEncryptedDatabase(file, 'k3y');
    await db.writeMeta('x', 'y');
    await db.close();

    final wrong = openEncryptedDatabase(file, 'other');
    addTearDown(wrong.close);
    await expectLater(wrong.readMeta('x'), throwsA(anything));
  });

  test('the bundled SQLite supports encryption', () {
    final db = sqlite3.openInMemory();
    addTearDown(db.close);
    expect(hasCipherSupport(db), isTrue);
  });
}
