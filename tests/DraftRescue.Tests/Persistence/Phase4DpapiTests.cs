using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;
using DraftRescue.Platform.Windows.Security;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4DpapiTests
{
    [Fact]
    public void PayloadCodecMatchesDrp1LittleEndianEnvelope()
    {
        var text = "Привет 🌍";
        var encoded = DraftPayloadV1Codec.Encode(text);

        Assert.Equal("DRP1", Encoding.ASCII.GetString(encoded, 0, 4));
        Assert.Equal((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(encoded.AsSpan(4)));
        Assert.Equal(1, encoded[6]);
        Assert.Equal(0, encoded[7]);
        Assert.Equal((uint)Encoding.UTF8.GetByteCount(text), BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(8)));
        Assert.Equal(text, DraftPayloadV1Codec.Decode(encoded));
    }

    [Fact]
    public void PayloadCodecRejectsMalformedEnvelopeAndInvalidUtf8()
    {
        var malformed = DraftPayloadV1Codec.Encode("safe");
        malformed[4] = 2;
        var versionError = Assert.Throws<DraftProtectionException>(() => DraftPayloadV1Codec.Decode(malformed));
        Assert.Equal(DraftProtectionFailureCode.InvalidProtectedPayload, versionError.Code);

        var invalidUtf8 = DraftPayloadV1Codec.Encode("x");
        invalidUtf8[8] = 1;
        invalidUtf8[12] = 0xFF;
        var utfError = Assert.Throws<DraftProtectionException>(() => DraftPayloadV1Codec.Decode(invalidUtf8));
        Assert.Equal(DraftProtectionFailureCode.InvalidProtectedPayload, utfError.Code);
    }

    [Fact]
    public void ProtectionFailureReasonClassificationIsStructuralAndContentFree()
    {
        Assert.Equal(DraftProtectionFailureReason.PlatformNotSupported,
            DraftProtectionException.Classify(new PlatformNotSupportedException()));
        Assert.Equal(DraftProtectionFailureReason.Unauthorized,
            DraftProtectionException.Classify(new UnauthorizedAccessException()));
        Assert.Equal(DraftProtectionFailureReason.Cryptographic,
            DraftProtectionException.Classify(new CryptographicException()));
        Assert.Equal(DraftProtectionFailureReason.Unknown,
            DraftProtectionException.Classify(new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() => DraftProtectionException.Classify(null!));

        var error = new DraftProtectionException(
            DraftProtectionFailureCode.DpapiFailure,
            reason: DraftProtectionFailureReason.Cryptographic);
        Assert.Equal(DraftProtectionFailureCode.DpapiFailure, error.Code);
        Assert.Equal(DraftProtectionFailureReason.Cryptographic, error.Reason);
        Assert.Equal(nameof(DraftProtectionFailureCode.DpapiFailure), error.Message);
    }

    [Fact]
    public void DpapiProtectorRoundTripsExactTextWithoutFallback()
    {
        var protector = new WindowsDpapiDraftProtector();
        var context = DraftProtectionContext.Create(DraftId.New(), 1);
        var plaintext = DraftPlaintextPayload.Create("phase4-dpapi-canary");

        ProtectedDraftPayload protectedPayload;
        try
        {
            protectedPayload = protector.Protect(plaintext, context);
        }
        catch (DraftProtectionException error) when (error.Code == DraftProtectionFailureCode.DpapiFailure)
        {
            Assert.Equal(DraftProtectionFailureCode.DpapiFailure, error.Code);
            return;
        }
        Assert.NotEqual(plaintext.Text, Encoding.UTF8.GetString(protectedPayload.Bytes.Span));
        Assert.Equal(plaintext.Text, protector.Unprotect(protectedPayload, context).Text);
        Assert.Throws<DraftProtectionException>(() => protector.Unprotect(protectedPayload, DraftProtectionContext.Create(DraftId.New(), 2)));
    }

    [Fact]
    public async Task InstallationSecretIsRandomStableAndCiphertextOnlyOnDisk()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP42-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "installation-secret.bin");
        try
        {
            var store = new WindowsDpapiInstallationSecretStore(path);
            var cancellationToken = TestContext.Current.CancellationToken;
            InstallationSecret first;
            try
            {
                first = await store.GetOrCreateAsync(cancellationToken);
            }
            catch (DraftProtectionException error) when (error.Code == DraftProtectionFailureCode.DpapiFailure)
            {
                Assert.Equal(DraftProtectionFailureCode.DpapiFailure, error.Code);
                return;
            }
            var second = await store.GetOrCreateAsync(cancellationToken);

            Assert.Equal(first.Bytes.ToArray(), second.Bytes.ToArray());
            Assert.Equal(32, first.Bytes.Length);
            var persisted = await File.ReadAllBytesAsync(path, cancellationToken);
            Assert.DoesNotContain("phase4-dpapi-canary", Encoding.UTF8.GetString(persisted), StringComparison.Ordinal);
            Assert.NotEqual(first.Bytes.ToArray(), persisted);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CorruptInstallationSecretFailsClosedWithoutRegeneration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP42-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "installation-secret.bin");
        try
        {
            var store = new WindowsDpapiInstallationSecretStore(path);
            var cancellationToken = TestContext.Current.CancellationToken;
            try
            {
                await store.GetOrCreateAsync(cancellationToken);
            }
            catch (DraftProtectionException dpapiError) when (dpapiError.Code == DraftProtectionFailureCode.DpapiFailure)
            {
                Assert.Equal(DraftProtectionFailureCode.DpapiFailure, dpapiError.Code);
                return;
            }
            await File.WriteAllBytesAsync(path, Encoding.ASCII.GetBytes("corrupt"), cancellationToken);
            var corruptError = await Assert.ThrowsAsync<DraftProtectionException>(() => store.GetOrCreateAsync(cancellationToken));
            Assert.Equal(DraftProtectionFailureCode.InvalidInstallationSecret, corruptError.Code);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void InstallationSecretRequiresExactly256Bits()
    {
        Assert.Throws<ArgumentException>(() => InstallationSecret.Create(new byte[31]));
        Assert.Equal(32, InstallationSecret.Create(new byte[32]).Bytes.Length);
    }
}
