import 'dart:io';

import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:paxscan_core/src/data/db/tables.dart';
import 'package:sqlite3/sqlite3.dart';

part 'app_database.g.dart';

enum OutboxState {
  saved,
  sending,
  acked;

  static OutboxState fromName(String value) => OutboxState.values.byName(value);
}

@DriftDatabase(tables: [Tasks, Outbox, Meta])
class AppDatabase extends _$AppDatabase {
  new(super.e);

  @override
  int get schemaVersion => 1;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    beforeOpen: (details) async {
      await customStatement('PRAGMA foreign_keys = ON');
    },
  );

  Future<String?> readMeta(String key) async =>
      (await (select(meta)..where((m) => m.key.equals(key))).getSingleOrNull())?.value;

  Future<void> writeMeta(String key, String value) =>
      into(meta).insertOnConflictUpdate(MetaCompanion.insert(key: key, value: value));

  Future<void> deleteMeta(Iterable<String> keys) =>
      (delete(meta)..where((m) => m.key.isIn(keys))).go();
}

/// Opens the on-device database encrypted with SQLite3MultipleCiphers.
///
/// [key] comes from the platform keystore; the file is unreadable without it.
AppDatabase openEncryptedDatabase(File file, String key) {
  return AppDatabase(
    NativeDatabase.createInBackground(
      file,
      setup: (db) {
        if (!hasCipherSupport(db)) {
          throw StateError(
            'SQLite3MultipleCiphers is not bundled; check hooks.user_defines.sqlite3.source in the workspace pubspec.',
          );
        }
        db
          ..execute("PRAGMA key = '${_escape(key)}';")
          ..execute('PRAGMA journal_mode = WAL;')
          ..execute('PRAGMA synchronous = FULL;');
      },
    ),
  );
}

/// True when the bundled SQLite supports encryption (`PRAGMA cipher` exists).
bool hasCipherSupport(Database db) => db.select('PRAGMA cipher;').isNotEmpty;

String _escape(String value) => value.replaceAll("'", "''");
