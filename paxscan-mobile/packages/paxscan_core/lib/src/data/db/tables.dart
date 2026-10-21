import 'package:drift/drift.dart';

/// Snapshot of tasks received from the hub, plus local optimistic status changes.
@DataClassName('TaskRow')
class Tasks extends Table {
  TextColumn get id => text()();
  TextColumn get name => text()();
  TextColumn get type => text()();
  TextColumn get description => text().nullable()();
  TextColumn get status => text()();

  /// Hub version; bumped locally on each status change so consecutive offline
  /// changes on this device are not reported as conflicts.
  IntColumn get version => integer()();
  DateTimeColumn get updatedAt => dateTime()();

  @override
  Set<Column<Object>> get primaryKey => {id};
}

/// Status updates waiting for (or already confirmed by) the hub.
/// Rows are never edited except for delivery bookkeeping.
@DataClassName('OutboxEntry')
class Outbox extends Table {
  TextColumn get id => text()();
  TextColumn get taskId => text()();
  TextColumn get fromStatus => text()();
  TextColumn get toStatus => text()();
  IntColumn get baseVersion => integer()();
  IntColumn get deviceSeq => integer().unique()();
  DateTimeColumn get deviceCreatedAt => dateTime()();

  /// `saved` → `sending` → `acked`.
  TextColumn get state => text()();
  IntColumn get attempts => integer().withDefault(const Constant(0))();
  TextColumn get lastError => text().nullable()();

  /// Hub result once acked: accepted, already_received, conflict, rejected.
  TextColumn get result => text().nullable()();
  TextColumn get resultReason => text().nullable()();
  DateTimeColumn get ackedAt => dateTime().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {id};
}

/// Small key/value store for pairing and sync bookkeeping (no secrets).
class Meta extends Table {
  TextColumn get key => text()();
  TextColumn get value => text()();

  @override
  Set<Column<Object>> get primaryKey => {key};
}
