/// Task status; [wire] is the API value (`in_progress`, ...).
enum TaskStatus {
  pending('pending', 'Pending'),
  inProgress('in_progress', 'In progress'),
  done('done', 'Done');

  new(this.wire, this.label);

  final String wire;
  final String label;

  static TaskStatus fromWire(String value) => TaskStatus.values.firstWhere(
    (s) => s.wire == value,
    orElse: () => throw FormatException('Unknown task status: $value'),
  );
}

enum TaskType {
  chef('chef', 'Chef'),
  guard('guard', 'Guard');

  new(this.wire, this.label);

  final String wire;
  final String label;

  static TaskType fromWire(String value) => TaskType.values.firstWhere(
    (t) => t.wire == value,
    orElse: () => throw FormatException('Unknown task type: $value'),
  );
}
