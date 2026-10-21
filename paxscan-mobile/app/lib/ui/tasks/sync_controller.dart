import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_core/paxscan_core.dart';

final class SyncStatus {
  const new({this.running = false, this.lastReport, this.lastError});

  final bool running;
  final SyncReport? lastReport;
  final Failure? lastError;
}

final syncControllerProvider = NotifierProvider<SyncController, SyncStatus>(SyncController.new);

/// "Sync now" from the UI; the heartbeat can also trigger syncs in the background.
class SyncController extends Notifier<SyncStatus> {
  @override
  SyncStatus build() => const SyncStatus();

  Future<void> syncNow() async {
    if (state.running) {
      return;
    }
    state = SyncStatus(running: true, lastReport: state.lastReport);
    final result = await ref.read(syncEngineProvider).sync();
    state = switch (result) {
      Ok(:final value) => SyncStatus(lastReport: value),
      Err(:final failure) => SyncStatus(lastReport: state.lastReport, lastError: failure),
    };
  }
}
