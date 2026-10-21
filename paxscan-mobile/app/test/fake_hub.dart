import 'package:paxscan_core/paxscan_core.dart';

/// Minimal in-memory hub: accepts pairing and updates, serves [tasks].
final class FakeHub implements HubApiClient {
  List<TaskDto> tasks = [];
  final List<StatusUpdateDto> received = [];

  @override
  Future<PairResponse> pair({
    required String pairingToken,
    required String deviceName,
    required String platform,
    required String appVersion,
  }) async => const PairResponse(deviceId: 'device-1', deviceSecret: 'secret', hubId: 'hub-1');

  @override
  Future<PingResponse> ping({required int pendingCount, required int lastSeq}) async =>
      PingResponse(syncRequested: false, serverTime: DateTime.utc(2026, 10, 15));

  @override
  Future<HelloResponse> hello({required int lastSeq, required int pendingCount, required DateTime deviceTime}) async =>
      HelloResponse(expectedFromSeq: 1, serverTime: DateTime.utc(2026, 10, 15));

  @override
  Future<PushResponse> push(List<StatusUpdateDto> items) async {
    received.addAll(items);
    tasks = [
      for (final t in tasks)
        switch (items.where((i) => i.taskId == t.id).lastOrNull) {
          final update? => TaskDto(
            id: t.id,
            name: t.name,
            type: t.type,
            description: t.description,
            status: update.toStatus,
            version: t.version + 1,
            updatedAt: t.updatedAt,
          ),
          null => t,
        },
    ];
    return PushResponse(
      results: [for (final i in items) ItemResultDto(id: i.id, result: 'accepted', taskVersion: i.baseVersion + 1)],
      seqGapDetected: false,
    );
  }

  @override
  Future<SnapshotResponse> snapshot() async => SnapshotResponse(datasetVersion: 'v1', tasks: tasks);
}

TaskDto task(String id, String name, TaskType type, {TaskStatus status = TaskStatus.pending}) => TaskDto(
  id: id,
  name: name,
  type: type,
  description: '$name description',
  status: status,
  version: 1,
  updatedAt: DateTime.utc(2026, 10, 15),
);
