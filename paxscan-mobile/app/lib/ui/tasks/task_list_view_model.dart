import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:paxscan_app/core/providers.dart';
import 'package:paxscan_core/paxscan_core.dart';

final class TaskFilter {
  const new({this.type, this.status});

  final TaskType? type;
  final TaskStatus? status;
}

final taskFilterProvider = NotifierProvider<TaskFilterNotifier, TaskFilter>(TaskFilterNotifier.new);

class TaskFilterNotifier extends Notifier<TaskFilter> {
  @override
  TaskFilter build() => const TaskFilter();

  void setType(TaskType? type) => state = TaskFilter(type: type, status: state.status);

  void setStatus(TaskStatus? status) => state = TaskFilter(type: state.type, status: status);
}

/// Demo: every device sees all tasks. The real app filters by the user's role here.
final filteredTasksProvider = StreamProvider<List<TaskRow>>((ref) {
  final filter = ref.watch(taskFilterProvider);
  return ref.watch(taskRepositoryProvider).watchTasks(type: filter.type, status: filter.status);
});

// Riverpod does not export the family provider type publicly.
// ignore: specify_nonobvious_property_types
final taskProvider = StreamProvider.family<TaskRow?, String>(
  (ref, id) => ref.watch(taskRepositoryProvider).watchTask(id),
);
