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

/// <summary>Stable, content-free protection failure. Exception text never includes payload data.</summary>
public sealed class DraftProtectionException : Exception
{
    public DraftProtectionException(DraftProtectionFailureCode code, Exception? innerException = null)
        : base(code.ToString(), innerException) => Code = code;

    public DraftProtectionFailureCode Code { get; }
}
