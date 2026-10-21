import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:paxscan_app/ui/pairing/pair_view_model.dart';

/// Camera QR scanner; replaced in widget tests (no camera plugin there).
final qrScannerProvider = Provider<Widget Function(ValueChanged<String> onCode)>(
  (ref) => (onCode) => MobileScanner(
    onDetect: (capture) {
      final raw = capture.barcodes.firstOrNull?.rawValue;
      if (raw != null) {
        onCode(raw);
      }
    },
  ),
);

class PairView extends ConsumerWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(pairViewModelProvider);
    final viewModel = ref.read(pairViewModelProvider.notifier);

    return Scaffold(
      appBar: AppBar(title: const Text('Pair with hub')),
      body: switch (state) {
        PairScanning(:final error) => _Scanner(error: error, onCode: viewModel.onCode),
        PairConfirming(:final payload) => _Confirm(
          hubAddress: payload.hubUrl.authority,
          onPair: viewModel.pair,
          onCancel: viewModel.cancel,
        ),
        PairInProgress() => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _Scanner extends StatelessWidget {
  const new({required this.error, required this.onCode});

  final String? error;
  final ValueChanged<String> onCode;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: Text(
            'On the desktop, open Devices → Pair new device and scan the QR code.',
            style: theme.textTheme.bodyLarge,
          ),
        ),
        if (error != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(error!, style: TextStyle(color: theme.colorScheme.error)),
          ),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: Consumer(builder: (context, ref, _) => ref.watch(qrScannerProvider)(onCode)),
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          child: TextButton.icon(
            icon: const Icon(Icons.content_paste),
            label: const Text('Enter code manually'),
            onPressed: () async {
              final code = await _askForCode(context);
              if (code != null) {
                onCode(code);
              }
            },
          ),
        ),
      ],
    );
  }

  /// Fallback for devices without a camera (e.g. emulators): paste the QR text.
  Future<String?> _askForCode(BuildContext context) {
    final controller = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Pairing code'),
        content: TextField(
          controller: controller,
          maxLines: 4,
          decoration: const InputDecoration(hintText: 'Paste the pairing payload shown by the hub'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(context, controller.text), child: const Text('Use code')),
        ],
      ),
    ).whenComplete(controller.dispose);
  }
}

class _Confirm extends StatefulWidget {
  const new({required this.hubAddress, required this.onPair, required this.onCancel});

  final String hubAddress;
  final Future<void> Function(String deviceName) onPair;
  final VoidCallback onCancel;

  @override
  State<_Confirm> createState() => _ConfirmState();
}

class _ConfirmState extends State<_Confirm> {
  final _name = TextEditingController();

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        const Icon(Icons.qr_code_2, size: 64),
        const SizedBox(height: 16),
        Text('Hub found at ${widget.hubAddress}', textAlign: TextAlign.center),
        const SizedBox(height: 24),
        TextField(
          controller: _name,
          autofocus: true,
          textInputAction: TextInputAction.done,
          decoration: const InputDecoration(
            labelText: 'Device name',
            hintText: 'e.g. Chef station 2',
            border: OutlineInputBorder(),
          ),
          onSubmitted: widget.onPair,
        ),
        const SizedBox(height: 16),
        FilledButton(onPressed: () => widget.onPair(_name.text), child: const Text('Pair')),
        TextButton(onPressed: widget.onCancel, child: const Text('Scan again')),
      ],
    );
  }
}
