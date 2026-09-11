namespace DraftRescue.Application.Security;

public enum CaptureDenialReason
{
    None = 0,
    SecureField = 1,
    CredentialLikeField = 2,
    BankingOrFinancialField = 3,
    PrivateBrowsing = 4,
    UnsupportedContext = 5,
    UncertainContext = 6,
    UnsupportedProfile = 7,
    UncertainPrivateMode = 8,
    UncertainSecureState = 9,
    StaleContext = 10,
    ProviderUnavailable = 11,
    InsufficientPositiveEvidence = 12
}
