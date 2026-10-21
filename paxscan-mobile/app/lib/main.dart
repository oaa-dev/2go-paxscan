import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/app.dart';
import 'package:paxscan_app/core/bootstrap.dart';
import 'package:paxscan_app/core/flutter_secure_store.dart';
import 'package:paxscan_app/core/providers.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  const secureStore = FlutterSecureStore();
  final db = await openAppDatabase(secureStore);

  final container = ProviderContainer(
    overrides: [
      secureStoreProvider.overrideWithValue(secureStore),
      databaseProvider.overrideWithValue(db),
    ],
  );
  container.read(pairedHubProvider.notifier).set(await container.read(pairingRepositoryProvider).currentHub());

  runApp(UncontrolledProviderScope(container: container, child: const PaxScanApp()));
}
