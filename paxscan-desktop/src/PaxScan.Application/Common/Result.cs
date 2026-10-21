namespace PaxScan.Application.Common;

public sealed record AppError(string Code, string Message);

/// <summary>Outcome of a use case. Expected failures are values, not exceptions.</summary>
public sealed class Result<T>
{
    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public AppError? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(string code, string message) => new(default, new AppError(code, message));

    public static implicit operator Result<T>(T value) => Success(value);
}

public static class ErrorCodes
{
    public const string PairingTokenInvalid = "pairing_token_invalid";
    public const string DeviceNotFound = "device_not_found";
    public const string DeviceRevoked = "device_revoked";
    public const string WebNotConfigured = "web_not_configured";
    public const string WebUnavailable = "web_unavailable";
    public const string ChecksumMismatch = "checksum_mismatch";
    public const string ValidationFailed = "validation_failed";
}
