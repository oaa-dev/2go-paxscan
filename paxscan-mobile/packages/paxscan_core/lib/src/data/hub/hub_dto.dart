import 'package:paxscan_core/src/domain/task_enums.dart';

// Wire shapes for the hub API (contracts/openapi.yaml, tag hub-device).

final class PairResponse {
  const new({required this.deviceId, required this.deviceSecret, required this.hubId});

  factory fromJson(Map<String, dynamic> json) => PairResponse(
    deviceId: json['device_id'] as String,
    deviceSecret: json['device_secret'] as String,
    hubId: json['hub_id'] as String,
  );

  final String deviceId;
  final String deviceSecret;
  final String hubId;
}

final class PingResponse {
  const new({required this.syncRequested, required this.serverTime});

  factory fromJson(Map<String, dynamic> json) => PingResponse(
    syncRequested: json['sync_requested'] as bool,
    serverTime: DateTime.parse(json['server_time'] as String),
  );

  final bool syncRequested;
  final DateTime serverTime;
}

final class HelloResponse {
  const new({required this.expectedFromSeq, required this.serverTime});

  factory fromJson(Map<String, dynamic> json) => HelloResponse(
    expectedFromSeq: (json['expected_from_seq'] as num).toInt(),
    serverTime: DateTime.parse(json['server_time'] as String),
  );

  final int expectedFromSeq;
  final DateTime serverTime;
}

final class StatusUpdateDto {
  const new({
    required this.id,
    required this.taskId,
    required this.fromStatus,
    required this.toStatus,
    required this.baseVersion,
    required this.deviceSeq,
    required this.deviceCreatedAt,
  });

  final String id;
  final String taskId;
  final TaskStatus fromStatus;
  final TaskStatus toStatus;
  final int baseVersion;
  final int deviceSeq;
  final DateTime deviceCreatedAt;

  Map<String, dynamic> toJson() => {
    'id': id,
    'task_id': taskId,
    'from_status': fromStatus.wire,
    'to_status': toStatus.wire,
    'base_version': baseVersion,
    'device_seq': deviceSeq,
    'device_created_at': deviceCreatedAt.toUtc().toIso8601String(),
  };
}

final class ItemResultDto {
  const new({required this.id, required this.result, this.reason, this.taskVersion});

  factory fromJson(Map<String, dynamic> json) => ItemResultDto(
    id: json['id'] as String,
    result: json['result'] as String,
    reason: json['reason'] as String?,
    taskVersion: (json['task_version'] as num?)?.toInt(),
  );

  final String id;

  /// accepted | already_received | conflict | rejected
  final String result;
  final String? reason;
  final int? taskVersion;
}

final class PushResponse {
  const new({required this.results, required this.seqGapDetected});

  factory fromJson(Map<String, dynamic> json) => PushResponse(
    results: (json['results'] as List<dynamic>)
        .map((r) => ItemResultDto.fromJson(r as Map<String, dynamic>))
        .toList(),
    seqGapDetected: json['seq_gap_detected'] as bool? ?? false,
  );

  final List<ItemResultDto> results;
  final bool seqGapDetected;
}

final class TaskDto {
  const new({
    required this.id,
    required this.name,
    required this.type,
    required this.description,
    required this.status,
    required this.version,
    required this.updatedAt,
  });

  factory fromJson(Map<String, dynamic> json) => TaskDto(
    id: json['id'] as String,
    name: json['name'] as String,
    type: TaskType.fromWire(json['type'] as String),
    description: json['description'] as String?,
    status: TaskStatus.fromWire(json['status'] as String),
    version: (json['version'] as num).toInt(),
    updatedAt: DateTime.parse(json['updated_at'] as String),
  );

  final String id;
  final String name;
  final TaskType type;
  final String? description;
  final TaskStatus status;
  final int version;
  final DateTime updatedAt;
}

final class SnapshotResponse {
  const new({required this.datasetVersion, required this.tasks});

  factory fromJson(Map<String, dynamic> json) => SnapshotResponse(
    datasetVersion: json['dataset_version'] as String?,
    tasks: (json['tasks'] as List<dynamic>).map((t) => TaskDto.fromJson(t as Map<String, dynamic>)).toList(),
  );

  final String? datasetVersion;
  final List<TaskDto> tasks;
}
