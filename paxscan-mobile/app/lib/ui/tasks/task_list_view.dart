import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/routing/router.dart';
import 'package:paxscan_app/ui/core/theme.dart';
import 'package:paxscan_app/ui/tasks/sync_controller.dart';
import 'package:paxscan_app/ui/tasks/task_list_view_model.dart';
import 'package:paxscan_app/ui/tasks/widgets/hub_banner.dart';
import 'package:paxscan_app/ui/tasks/widgets/status_chip.dart';
import 'package:paxscan_core/paxscan_core.dart';

class TaskListView extends ConsumerWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasks = ref.watch(filteredTasksProvider);
    final filter = ref.watch(taskFilterProvider);
    final unsynced = ref.watch(unsyncedCountProvider).value ?? 0;
    final syncing = ref.watch(syncControllerProvider.select((s) => s.running));

    ref.listen(syncControllerProvider.select((s) => s.lastError), (_, error) {
      if (error != null) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Sync failed: ${error.message}')));
      }
    });

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tasks'),
        actions: [
          IconButton(
            tooltip: 'Sync now',
            onPressed: syncing ? null : () => ref.read(syncControllerProvider.notifier).syncNow(),
            icon: Badge(
              isLabelVisible: unsynced > 0,
              label: Text('$unsynced'),
              child: syncing
                  ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.sync),
            ),
          ),
          IconButton(
            tooltip: 'Settings',
            icon: const Icon(Icons.settings_outlined),
            onPressed: () => context.push(Routes.settings),
          ),
        ],
      ),
      body: Column(
        children: [
          const HubBanner(),
          _Filters(filter: filter),
          Expanded(
            child: tasks.when(
              data: (rows) => rows.isEmpty
                  ? const _Empty()
                  : RefreshIndicator(
                      onRefresh: () => ref.read(syncControllerProvider.notifier).syncNow(),
                      child: ListView.separated(
                        itemCount: rows.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (context, i) => _TaskTile(rows[i]),
                      ),
                    ),
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => Center(child: Text('Could not load tasks: $error')),
            ),
          ),
        ],
      ),
    );
  }
}

class _Filters extends ConsumerWidget {
  const new({required this.filter});

  final TaskFilter filter;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final notifier = ref.read(taskFilterProvider.notifier);
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: Row(
        spacing: 8,
        children: [
          for (final type in TaskType.values)
            FilterChip(
              avatar: Icon(AppTheme.typeIcon(type), size: 18),
              label: Text(type.label),
              selected: filter.type == type,
              onSelected: (on) => notifier.setType(on ? type : null),
            ),
          const SizedBox(width: 8),
          for (final status in TaskStatus.values)
            ChoiceChip(
              label: Text(status.label),
              selected: filter.status == status,
              onSelected: (on) => notifier.setStatus(on ? status : null),
            ),
        ],
      ),
    );
  }
}

class _TaskTile extends StatelessWidget {
  const new(this.task);

  final TaskRow task;

  @override
  Widget build(BuildContext context) {
    final type = TaskType.fromWire(task.type);
    return ListTile(
      leading: CircleAvatar(child: Icon(AppTheme.typeIcon(type))),
      title: Text(task.name),
      subtitle: task.description == null
          ? Text(type.label)
          : Text(task.description!, maxLines: 1, overflow: TextOverflow.ellipsis),
      trailing: StatusChip(TaskStatus.fromWire(task.status)),
      onTap: () => context.push(Routes.task(task.id)),
    );
  }
}

class _Empty extends ConsumerWidget {
  const new();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.inbox_outlined, size: 48),
            const SizedBox(height: 12),
            const Text('No tasks', textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: () => ref.read(syncControllerProvider.notifier).syncNow(),
              child: const Text('Sync with hub'),
            ),
          ],
        ),
      ),
    );
  }
}
