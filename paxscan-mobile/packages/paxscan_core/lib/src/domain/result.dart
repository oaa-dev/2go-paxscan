/// Outcome of an operation whose failure is expected (offline hub, bad QR, ...).
sealed class Result<T> {
  const new();

  bool get isOk => this is Ok<T>;
}

final class Ok<T> extends Result<T> {
  const new(this.value);

  final T value;
}

final class Err<T> extends Result<T> {
  const new(this.failure);

  final Failure failure;
}

enum FailureKind {
  /// Hub not reachable (Wi-Fi, firewall, hub closed). Work continues offline.
  unreachable,

  /// The hub's certificate does not match the pinned fingerprint.
  untrustedHub,

  /// Device revoked or credentials rejected; it must pair again.
  unauthorized,

  /// Invalid input (bad QR code, expired pairing code, ...).
  invalid,

  /// Operation not allowed in the current state.
  blocked,

  /// Anything else.
  unexpected,
}

final class Failure {
  const new(this.kind, this.message);

  final FailureKind kind;
  final String message;

  @override
  String toString() => 'Failure($kind, $message)';
}
