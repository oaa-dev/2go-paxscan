import 'dart:convert';

import 'package:paxscan_core/src/domain/result.dart';

/// Contents of the hub's pairing QR code: `{v, hub_url, cert_sha256, pairing_token, hub_id}`.
final class PairingPayload {
  const new({required this.hubUrl, required this.certSha256, required this.pairingToken, required this.hubId});

  final Uri hubUrl;
  final String certSha256;
  final String pairingToken;
  final String hubId;

  static final _fingerprint = RegExp(r'^[0-9a-f]{64}$');

  static Result<PairingPayload> parse(String raw) {
    const invalid = Err<PairingPayload>(Failure(FailureKind.invalid, 'This is not a PaxScan pairing code.'));
    final Object? decoded;
    try {
      decoded = jsonDecode(raw);
    } on FormatException {
      return invalid;
    }

    if (decoded case {
      'v': 1,
      'hub_url': final String url,
      'cert_sha256': final String cert,
      'pairing_token': final String token,
      'hub_id': final String hubId,
    }) {
      final hubUrl = Uri.tryParse(url);
      if (hubUrl == null || hubUrl.scheme != 'https' || hubUrl.host.isEmpty) {
        return const Err(Failure(FailureKind.invalid, 'The pairing code has an invalid hub address.'));
      }
      if (!_fingerprint.hasMatch(cert.toLowerCase()) || token.isEmpty) {
        return invalid;
      }
      return Ok(PairingPayload(hubUrl: hubUrl, certSha256: cert.toLowerCase(), pairingToken: token, hubId: hubId));
    }
    return invalid;
  }
}
