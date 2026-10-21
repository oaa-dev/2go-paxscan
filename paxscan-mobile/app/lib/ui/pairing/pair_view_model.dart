import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/app_info.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_core/paxscan_core.dart';

sealed class PairState {
  const new();
}

/// Waiting for a QR code.
final class PairScanning extends PairState {
  const new({this.error});

  final String? error;
}

/// QR read; asking for the device name.
final class PairConfirming extends PairState {
  const new(this.payload);

  final PairingPayload payload;
}

final class PairInProgress extends PairState {
  const new();
}

final NotifierProvider<PairViewModel, PairState> pairViewModelProvider = NotifierProvider.autoDispose<PairViewModel, PairState>(PairViewModel.new);

class PairViewModel extends Notifier<PairState> {
  @override
  PairState build() => const PairScanning();

  /// Called with each scanned (or pasted) code; ignored unless scanning.
  void onCode(String raw) {
    if (state is! PairScanning) {
      return;
    }
    switch (PairingPayload.parse(raw)) {
      case Ok(:final value):
        state = PairConfirming(value);
      case Err(:final failure):
        state = PairScanning(error: failure.message);
    }
  }

  void cancel() => state = const PairScanning();

  Future<void> pair(String deviceName) async {
    final current = state;
    if (current is! PairConfirming) {
      return;
    }
    final name = deviceName.trim();
    if (name.isEmpty) {
      state = const PairScanning(error: 'Enter a device name.');
      return;
    }

    state = const PairInProgress();
    final result = await ref.read(pairingRepositoryProvider).pair(
      payload: current.payload,
      deviceName: name,
      platform: AppInfo.platform,
      appVersion: AppInfo.version,
    );

    switch (result) {
      case Ok(:final value):
        ref.read(pairedHubProvider.notifier).set(value);
        // First download of tasks; failures are shown on the task list.
        await ref.read(syncEngineProvider).sync();
      case Err(:final failure):
        state = PairScanning(error: failure.message);
    }
  }
}
