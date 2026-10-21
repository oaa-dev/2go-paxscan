import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/routing/router.dart';
import 'package:paxscan_app/ui/core/theme.dart';
import 'package:paxscan_core/paxscan_core.dart';

class PaxScanApp extends ConsumerStatefulWidget {
  const new({super.key});

  @override
  ConsumerState<PaxScanApp> createState() => _PaxScanAppState();
}

class _PaxScanAppState extends ConsumerState<PaxScanApp> {
  late final AppLifecycleListener _lifecycle;
  late final Heartbeat _heartbeat;

  @override
  void initState() {
    super.initState();
    _heartbeat = ref.read(heartbeatProvider);
    // Heartbeat only while visible; a resume also triggers one immediately.
    _lifecycle = AppLifecycleListener(onResume: _startHeartbeat, onPause: _heartbeat.stop);
    _startHeartbeat();
  }

  void _startHeartbeat() {
    if (ref.read(pairedHubProvider) != null) {
      _heartbeat.start();
    }
  }

  @override
  void dispose() {
    _heartbeat.stop();
    _lifecycle.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(pairedHubProvider, (_, hub) => hub == null ? _heartbeat.stop() : _heartbeat.start());

    return MaterialApp.router(
      title: 'PaxScan',
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
