using System.Security.Cryptography;
using System.Text;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;

namespace DraftRescue.Platform.Windows.Security;

/// <summary>Windows DPAPI CurrentUser implementation for the strict DRP1 payload envelope.</summary>
public sealed class WindowsDpapiDraftProtector : IDraftProtector
{
    public ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateContext(context);
        var plaintextBytes = DraftPayloadV1Codec.Encode(plaintext.Text);
        try
        {
            byte[] protectedBytes;
            try
            {
                protectedBytes = ProtectedData.Protect(plaintextBytes, Array.Empty<byte>(), DataProtectionScope.CurrentUser);
            }
            catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException or UnauthorizedAccessException)
            {
                throw new DraftProtectionException(DraftProtectionFailureCode.DpapiFailure, ex);
            }

            return ProtectedDraftPayload.Create(context.ProtectionVersion, protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    public DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context)
    {
        ArgumentNullException.ThrowIfNull(protectedPayload);
        ValidateContext(context);
        if (protectedPayload.ProtectionVersion != context.ProtectionVersion)
            throw new DraftProtectionException(DraftProtectionFailureCode.UnsupportedProtectionVersion);

        byte[] decrypted;
        try
        {
            decrypted = ProtectedData.Unprotect(protectedPayload.Bytes.ToArray(), Array.Empty<byte>(), DataProtectionScope.CurrentUser);
        }
        catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            throw new DraftProtectionException(DraftProtectionFailureCode.DpapiFailure, ex);
        }

        try
        {
            return DraftPlaintextPayload.Create(DraftPayloadV1Codec.Decode(decrypted));
        }
        catch (DraftProtectionException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or DecoderFallbackException or OverflowException)
        {
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectedPayload, ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decrypted);
        }
    }

    private static void ValidateContext(DraftProtectionContext context)
    {
        if (context.DraftId.Value == Guid.Empty || context.ProtectionVersion <= 0)
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectionContext);
    }

}
