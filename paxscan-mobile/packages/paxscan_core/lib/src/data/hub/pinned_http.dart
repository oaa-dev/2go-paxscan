import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';

/// Lowercase hex SHA-256 of a DER certificate — the value the hub puts in its QR code.
String sha256Fingerprint(X509Certificate certificate) => sha256FingerprintOfDer(certificate.der);

String sha256FingerprintOfDer(List<int> der) => sha256.convert(der).toString();

/// A Dio client that trusts exactly one certificate: the hub's, identified by its
/// SHA-256 fingerprint. System CAs are not trusted and hostnames are not checked
/// (the hub is reached by LAN IP with a self-signed certificate).
Dio createPinnedDio({
  required Uri baseUrl,
  required String certSha256,
  Duration connectTimeout = const Duration(seconds: 5),
  Duration receiveTimeout = const Duration(seconds: 20),
}) {
  final expected = certSha256.toLowerCase();
  bool matches(X509Certificate? cert) => cert != null && sha256Fingerprint(cert) == expected;

  return Dio(
    BaseOptions(
      baseUrl: baseUrl.toString(),
      connectTimeout: connectTimeout,
      receiveTimeout: receiveTimeout,
      sendTimeout: receiveTimeout,
      contentType: Headers.jsonContentType,
    ),
  )..httpClientAdapter = IOHttpClientAdapter(
    createHttpClient: () {
      // No trusted roots: every certificate goes through the fingerprint check below.
      final client = HttpClient(context: SecurityContext())
        ..badCertificateCallback = ((cert, host, port) => matches(cert))
        ..connectionTimeout = connectTimeout;
      return client;
    },
    validateCertificate: (cert, host, port) => matches(cert),
  );
}
