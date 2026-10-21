import 'dart:io';

import 'package:dio/dio.dart';
import 'package:paxscan_core/src/data/hub/hub_dto.dart';
import 'package:paxscan_core/src/data/hub/pinned_http.dart';
import 'package:paxscan_core/src/domain/result.dart';

/// Where the hub is and how to trust it (from the pairing QR code).
final class HubConnection {
  const new({required this.hubUrl, required this.certSha256, this.deviceId, this.deviceSecret});

  final Uri hubUrl;
  final String certSha256;
  final String? deviceId;
  final String? deviceSecret;
}

/// Error from the hub API, already classified for the UI.
final class HubApiException implements Exception {
  const new(this.failure);

  final Failure failure;

  @override
  String toString() => 'HubApiException(${failure.kind}: ${failure.message})';
}

/// Hub API used by the sync engine. An interface so tests can fake the hub.
abstract interface class HubApiClient {
  Future<PairResponse> pair({
    required String pairingToken,
    required String deviceName,
    required String platform,
    required String appVersion,
  });

  Future<PingResponse> ping({required int pendingCount, required int lastSeq});

  Future<HelloResponse> hello({required int lastSeq, required int pendingCount, required DateTime deviceTime});

  Future<PushResponse> push(List<StatusUpdateDto> items);

  Future<SnapshotResponse> snapshot();
}

typedef HubApiClientFactory = HubApiClient Function(HubConnection connection);

/// Real client over HTTPS with the pinned hub certificate.
final class DioHubApiClient implements HubApiClient {
  new(HubConnection connection, {Dio? dio})
    : _dio = dio ?? createPinnedDio(baseUrl: connection.hubUrl, certSha256: connection.certSha256) {
    if (connection.deviceId != null && connection.deviceSecret != null) {
      _dio.options.headers[HttpHeaders.authorizationHeader] =
          'Device ${connection.deviceId}:${connection.deviceSecret}';
    }
  }

  final Dio _dio;

  @override
  Future<PairResponse> pair({
    required String pairingToken,
    required String deviceName,
    required String platform,
    required String appVersion,
  }) => _post('/v1/pair', {
    'pairing_token': pairingToken,
    'device_name': deviceName,
    'platform': platform,
    'app_version': appVersion,
  }, PairResponse.fromJson);

  @override
  Future<PingResponse> ping({required int pendingCount, required int lastSeq}) =>
      _post('/v1/ping', {'pending_count': pendingCount, 'last_seq': lastSeq}, PingResponse.fromJson);

  @override
  Future<HelloResponse> hello({required int lastSeq, required int pendingCount, required DateTime deviceTime}) => _post(
    '/v1/sync/hello',
    {'last_seq': lastSeq, 'pending_count': pendingCount, 'device_time': deviceTime.toUtc().toIso8601String()},
    HelloResponse.fromJson,
  );

  @override
  Future<PushResponse> push(List<StatusUpdateDto> items) =>
      _post('/v1/sync/push', {'items': items.map((i) => i.toJson()).toList()}, PushResponse.fromJson);

  @override
  Future<SnapshotResponse> snapshot() => _guard(() async {
    final response = await _dio.get<Map<String, dynamic>>('/v1/sync/snapshot');
    return SnapshotResponse.fromJson(response.data!);
  });

  Future<T> _post<T>(String path, Map<String, dynamic> body, T Function(Map<String, dynamic>) parse) => _guard(() async {
    final response = await _dio.post<Map<String, dynamic>>(path, data: body);
    return parse(response.data!);
  });

  Future<T> _guard<T>(Future<T> Function() call) async {
    try {
      return await call();
    } on DioException catch (e) {
      throw HubApiException(classify(e));
    }
  }

  /// Maps transport and HTTP errors to failures the UI can explain.
  static Failure classify(DioException e) {
    final status = e.response?.statusCode;
    final detail = switch (e.response?.data) {
      {'detail': final String d} => d,
      _ => null,
    };

    if (status == 401) {
      return Failure(FailureKind.unauthorized, detail ?? 'The hub no longer recognises this device. Pair it again.');
    }
    if (status != null && status >= 400 && status < 500) {
      return Failure(FailureKind.invalid, detail ?? 'The hub rejected the request (HTTP $status).');
    }
    if (e.type == DioExceptionType.badCertificate || e.error is HandshakeException) {
      return const Failure(FailureKind.untrustedHub, 'The hub certificate does not match the paired hub.');
    }
    if (e.type case DioExceptionType.connectionTimeout ||
        DioExceptionType.connectionError ||
        DioExceptionType.receiveTimeout ||
        DioExceptionType.sendTimeout) {
      return const Failure(FailureKind.unreachable, 'Hub not reachable. Check you are on the hub Wi-Fi.');
    }
    return Failure(FailureKind.unexpected, e.message ?? 'Unexpected hub error.');
  }
}
