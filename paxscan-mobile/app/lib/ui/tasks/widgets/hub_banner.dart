import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/ui/tasks/sync_controller.dart';
import 'package:paxscan_core/paxscan_core.dart';

/// Connection + sync state at the top of the task list. Work never depends on it.
class HubBanner extends ConsumerWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final scheme = Theme.of(context).colorScheme;
    final reachability = ref.watch(hubReachabilityProvider).value ?? HubReachability.unknown;
    final sync = ref.watch(syncControllerProvider);
    final unsynced = ref.watch(unsyncedCountProvider).value ?? 0;

    final (icon, color, text) = switch ((reachability, sync.lastError)) {
      (_, Failure(kind: FailureKind.unauthorized, :final message)) ||
      (HubReachability.unauthorized, Failure(:final message)) => (Icons.block, scheme.error, message),
      (HubReachability.unauthorized, null) => (Icons.block, scheme.error, 'This device was removed from the hub. Pair again in Settings.'),
      (_, Failure(kind: FailureKind.untrustedHub, :final message)) => (Icons.gpp_bad, scheme.error, message),
      (HubReachability.reachable, _) => (Icons.wifi, Colors.green.shade700, _syncedText(sync, unsynced)),
      (HubReachability.unreachable, _) => (Icons.wifi_off, scheme.outline, 'Working offline · $unsynced not synced yet'),
      _ => (Icons.sync, scheme.outline, _syncedText(sync, unsynced)),
    };

    return Material(
      color: color.withValues(alpha: 0.08),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Row(
          children: [
            Icon(icon, size: 18, color: color),
            const SizedBox(width: 8),
            Expanded(child: Text(text, style: TextStyle(color: color))),
          ],
        ),
      ),
    );
  }

  static String _syncedText(SyncStatus sync, int unsynced) {
    if (sync.running) {
      return 'Syncing…';
    }
    final pending = unsynced == 0 ? 'everything synced' : '$unsynced not synced yet';
    final report = sync.lastReport;
    if (report == null) {
      return pending;
    }
    final at = TimeOfDay.fromDateTime(report.finishedAt.toLocal());
    final conflicts = report.conflicts > 0 ? ' · ${report.conflicts} conflict(s) flagged' : '';
    return 'Last sync ${at.hour.toString().padLeft(2, '0')}:${at.minute.toString().padLeft(2, '0')} · $pending$conflicts';
  }
}
