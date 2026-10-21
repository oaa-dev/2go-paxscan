import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/ui/pairing/pair_view.dart';
import 'package:paxscan_app/ui/settings/settings_view.dart';
import 'package:paxscan_app/ui/tasks/task_detail_view.dart';
import 'package:paxscan_app/ui/tasks/task_list_view.dart';

abstract final class Routes {
  static const pair = '/pair';
  static const tasks = '/tasks';
  static const settings = '/settings';

  static String task(String id) => '/tasks/$id';
}

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run redirects whenever pairing changes, without rebuilding the router.
  final paired = ValueNotifier<bool>(ref.read(pairedHubProvider) != null);
  ref
    ..listen(pairedHubProvider, (_, hub) => paired.value = hub != null)
    ..onDispose(paired.dispose);

  final router = GoRouter(
    initialLocation: Routes.tasks,
    refreshListenable: paired,
    redirect: (context, state) {
      final atPair = state.matchedLocation == Routes.pair;
      if (!paired.value) {
        return atPair ? null : Routes.pair;
      }
      return atPair ? Routes.tasks : null;
    },
    routes: [
      GoRoute(path: Routes.pair, builder: (context, state) => const PairView()),
      GoRoute(
        path: Routes.tasks,
        builder: (context, state) => const TaskListView(),
        routes: [
          GoRoute(
            path: ':id',
            builder: (context, state) => TaskDetailView(taskId: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(path: Routes.settings, builder: (context, state) => const SettingsView()),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});
