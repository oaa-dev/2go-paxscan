import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_core/paxscan_core.dart';

/// Overridden in main() with the opened instances.
final secureStoreProvider = Provider<SecureStore>((ref) => throw UnimplementedError('override in main'));
final databaseProvider = Provider<AppDatabase>((ref) => throw UnimplementedError('override in main'));

/// Creates hub clients; overridden in tests with a fake hub.
final hubClientFactoryProvider = Provider<HubApiClientFactory>((ref) => DioHubApiClient.new);

final pairingRepositoryProvider = Provider<PairingRepository>(
  (ref) => PairingRepository(
    db: ref.watch(databaseProvider),
    secureStore: ref.watch(secureStoreProvider),
    clientFactory: ref.watch(hubClientFactoryProvider),
  ),
);

final taskRepositoryProvider = Provider<TaskRepository>((ref) => TaskRepository(ref.watch(databaseProvider)));

final syncEngineProvider = Provider<SyncEngine>(
  (ref) => SyncEngine(
    db: ref.watch(databaseProvider),
    pairing: ref.watch(pairingRepositoryProvider),
    clientFactory: ref.watch(hubClientFactoryProvider),
  ),
);

final heartbeatProvider = Provider<Heartbeat>((ref) {
  final heartbeat = Heartbeat(
    db: ref.watch(databaseProvider),
    pairing: ref.watch(pairingRepositoryProvider),
    clientFactory: ref.watch(hubClientFactoryProvider),
    syncEngine: ref.watch(syncEngineProvider),
  );
  ref.onDispose(heartbeat.dispose);
  return heartbeat;
});

final hubReachabilityProvider = StreamProvider<HubReachability>(
  (ref) => ref.watch(heartbeatProvider).reachability,
);

final unsyncedCountProvider = StreamProvider<int>((ref) => ref.watch(taskRepositoryProvider).watchUnsyncedCount());

/// The hub this device is paired with (null = not paired). Seeded in main().
final pairedHubProvider = NotifierProvider<PairedHubNotifier, PairedHub?>(PairedHubNotifier.new);

class PairedHubNotifier extends Notifier<PairedHub?> {
  @override
  PairedHub? build() => null;

  // A setter would hide that this is the single place the pairing state changes.
  // ignore: use_setters_to_change_properties
  void set(PairedHub? hub) => state = hub;
}
