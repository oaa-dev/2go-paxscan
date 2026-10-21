import 'dart:convert';

import 'package:drift/drift.dart' show driftRuntimeOptions;
import 'package:drift/native.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paxscan_app/app.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/ui/pairing/pair_view.dart';
import 'package:paxscan_core/paxscan_core.dart';

import 'fake_hub.dart';

void main() {
  late FakeHub hub;
  late AppDatabase db;
  late ProviderContainer container;

  final qrCode = jsonEncode({
    'v': 1,
    'hub_url': 'https://192.168.137.1:5443',
    'cert_sha256': 'ab' * 32,
    'pairing_token': 'token',
    'hub_id': 'hub-1',
  });

  setUp(() {
    driftRuntimeOptions.dontWarnAboutMultipleDatabases = true;
    hub = FakeHub()
      ..tasks = [
        task('t1', 'Prep breakfast', TaskType.chef),
        task('t2', 'Patrol deck 5', TaskType.guard),
      ];
    db = AppDatabase(NativeDatabase.memory());
    container = ProviderContainer(
      overrides: [
        databaseProvider.overrideWithValue(db),
        secureStoreProvider.overrideWithValue(MemorySecureStore()),
        hubClientFactoryProvider.overrideWithValue((_) => hub),
        // No camera in tests: the "scanner" is a button that delivers the QR text.
        qrScannerProvider.overrideWithValue(
          (onCode) => TextButton(onPressed: () => onCode(qrCode), child: const Text('fake-scan')),
        ),
      ],
    );
  });

  tearDown(() async {
    container.read(heartbeatProvider).stop();
    container.dispose();
    await db.close();
  });

  Future<void> startApp(WidgetTester tester) async {
    await tester.pumpWidget(UncontrolledProviderScope(container: container, child: const PaxScanApp()));
    await tester.pumpAndSettle();
  }

  Future<void> pair(WidgetTester tester) async {
    await tester.tap(find.text('fake-scan'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Chef station 1');
    await tester.tap(find.widgetWithText(FilledButton, 'Pair'));
    await tester.pumpAndSettle();
  }

  testWidgets('an unpaired device starts on the pairing screen', (tester) async {
    await startApp(tester);

    expect(find.text('Pair with hub'), findsOneWidget);
  });

  testWidgets('pairing downloads the tasks and shows them', (tester) async {
    await startApp(tester);

    await pair(tester);

    expect(find.text('Tasks'), findsOneWidget);
    expect(find.text('Prep breakfast'), findsOneWidget);
    expect(find.text('Patrol deck 5'), findsOneWidget);
  });

  testWidgets('a bad QR code shows an error and stays on the scanner', (tester) async {
    container.updateOverrides([
      databaseProvider.overrideWithValue(db),
      secureStoreProvider.overrideWithValue(MemorySecureStore()),
      hubClientFactoryProvider.overrideWithValue((_) => hub),
      qrScannerProvider.overrideWithValue(
        (onCode) => TextButton(onPressed: () => onCode('not a pairing code'), child: const Text('fake-scan')),
      ),
    ]);
    await startApp(tester);

    await tester.tap(find.text('fake-scan'));
    await tester.pumpAndSettle();

    expect(find.text('This is not a PaxScan pairing code.'), findsOneWidget);
  });

  testWidgets('changing a status offline queues it, and sync delivers it to the hub', (tester) async {
    await startApp(tester);
    await pair(tester);

    await tester.tap(find.text('Prep breakfast'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Done'));
    await tester.pumpAndSettle();
    expect(find.text('Saved on this device · will sync with the hub'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();
    expect(find.descendant(of: find.byType(Badge), matching: find.text('1')), findsOneWidget);

    await tester.tap(find.byTooltip('Sync now'));
    await tester.pumpAndSettle();

    expect(hub.received.single.toStatus, TaskStatus.done);
    expect(find.descendant(of: find.byType(Badge), matching: find.text('1')), findsNothing);
  });

  testWidgets('type filter narrows the list', (tester) async {
    await startApp(tester);
    await pair(tester);

    await tester.tap(find.widgetWithText(FilterChip, 'Guard'));
    await tester.pumpAndSettle();

    expect(find.text('Patrol deck 5'), findsOneWidget);
    expect(find.text('Prep breakfast'), findsNothing);
  });
}
