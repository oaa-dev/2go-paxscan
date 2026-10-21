import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/app_info.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_core/paxscan_core.dart';

class SettingsView extends ConsumerWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hub = ref.watch(pairedHubProvider);
    final unsynced = ref.watch(unsyncedCountProvider).value ?? 0;

    Future<void> unpair() async {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Unpair this device?'),
          content: const Text('Tasks are removed from this device. You will need to scan a new QR code.'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
            FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Unpair')),
          ],
        ),
      );
      if (confirmed != true) {
        return;
      }

      final result = await ref.read(pairingRepositoryProvider).unpair();
      switch (result) {
        case Ok():
          ref.read(pairedHubProvider.notifier).set(null);
        case Err(:final failure):
          if (context.mounted) {
            ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(failure.message)));
          }
      }
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          if (hub != null) ...[
            ListTile(title: const Text('Device name'), subtitle: Text(hub.deviceName)),
            ListTile(title: const Text('Hub address'), subtitle: Text(hub.hubUrl.toString())),
            ListTile(title: const Text('Hub ID'), subtitle: Text(hub.hubId)),
            ListTile(title: const Text('Device ID'), subtitle: Text(hub.deviceId)),
            ListTile(
              title: const Text('Hub certificate (pinned)'),
              subtitle: Text('${hub.certSha256.substring(0, 16)}…', style: const TextStyle(fontFamily: 'monospace')),
            ),
          ],
          ListTile(title: const Text('Updates not synced'), subtitle: Text('$unsynced')),
          const ListTile(title: Text('App version'), subtitle: Text(AppInfo.version)),
          const Divider(),
          Padding(
            padding: const EdgeInsets.all(16),
            child: OutlinedButton.icon(
              icon: const Icon(Icons.link_off),
              label: const Text('Unpair device'),
              onPressed: unpair,
            ),
          ),
        ],
      ),
    );
  }
}
