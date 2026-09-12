using System.Security.Cryptography;

namespace DraftRescue.Platform.Windows.Security;

public enum DraftProtectionFailureCode
{
    DpapiFailure = 1,
    InvalidProtectedPayload = 2,
    UnsupportedProtectionVersion = 3,
    InvalidProtectionContext = 4,
    InvalidInstallationSecret = 5,
    InstallationSecretUnavailable = 6
}

/// <summary>Audited, content-free reason for a platform protection failure.</summary>
public enum DraftProtectionFailureReason
{
    Unknown = 0,
    PlatformNotSupported = 1,
    Unauthorized = 2,
    Cryptographic = 3
}

/// <summary>Stable, content-free protection failure. Exception text never includes payload data.</summary>
public sealed class DraftProtectionException : Exception
{
    public DraftProtectionException(
        DraftProtectionFailureCode code,
        Exception? innerException = null,
        DraftProtectionFailureReason reason = DraftProtectionFailureReason.Unknown)
        : base(code.ToString(), innerException)
    {
        Code = code;
        Reason = reason;
    }

    public DraftProtectionFailureCode Code { get; }
    public DraftProtectionFailureReason Reason { get; }

    public static DraftProtectionFailureReason Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            PlatformNotSupportedException => DraftProtectionFailureReason.PlatformNotSupported,
            UnauthorizedAccessException => DraftProtectionFailureReason.Unauthorized,
            CryptographicException => DraftProtectionFailureReason.Cryptographic,
            _ => DraftProtectionFailureReason.Unknown
        };
    }
}
