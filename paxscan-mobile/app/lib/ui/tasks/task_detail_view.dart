import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_app/ui/core/theme.dart';
import 'package:paxscan_app/ui/tasks/task_list_view_model.dart';
import 'package:paxscan_core/paxscan_core.dart';

class TaskDetailView extends ConsumerWidget {
  const new({required this.taskId, super.key});

  final String taskId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final task = ref.watch(taskProvider(taskId));

    return Scaffold(
      appBar: AppBar(title: const Text('Task')),
      body: task.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(child: Text('$error')),
        data: (task) => task == null
            ? const Center(child: Text('This task no longer exists on the hub.'))
            : _Body(task: task),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const new({required this.task});

  final TaskRow task;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final type = TaskType.fromWire(task.type);
    final status = TaskStatus.fromWire(task.status);

    Future<void> change(TaskStatus to) async {
      final result = await ref.read(taskRepositoryProvider).changeStatus(task.id, to);
      if (!context.mounted) {
        return;
      }
      final message = switch (result) {
        Ok() => 'Saved on this device · will sync with the hub',
        Err(:final failure) => failure.message,
      };
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }

    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Row(
          children: [
            Icon(AppTheme.typeIcon(type), color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Text(type.label, style: theme.textTheme.labelLarge),
          ],
        ),
        const SizedBox(height: 8),
        Text(task.name, style: theme.textTheme.headlineSmall),
        if (task.description case final description?) ...[
          const SizedBox(height: 12),
          Text(description, style: theme.textTheme.bodyLarge),
        ],
        const SizedBox(height: 32),
        Text('Status', style: theme.textTheme.titleMedium),
        const SizedBox(height: 12),
        SegmentedButton<TaskStatus>(
          segments: [
            for (final s in TaskStatus.values) ButtonSegment(value: s, label: Text(s.label)),
          ],
          selected: {status},
          showSelectedIcon: false,
          onSelectionChanged: (selection) => change(selection.single),
        ),
        const SizedBox(height: 12),
        Text(
          'Changes are saved on this device immediately and sent to the hub on the next sync.',
          style: theme.textTheme.bodySmall,
        ),
      ],
    );
  }
}
