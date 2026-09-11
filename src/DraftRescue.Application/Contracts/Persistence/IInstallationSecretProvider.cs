namespace DraftRescue.Application.Contracts.Persistence;

/// <summary>Transient 256-bit HMAC key material. Implementations must never persist the returned bytes.</summary>
public interface IInstallationSecretProvider
{
    Task<InstallationSecret> GetOrCreateAsync(CancellationToken cancellationToken = default);
}

public sealed class InstallationSecret
{
    private readonly byte[] _bytes;

    private InstallationSecret(byte[] bytes) => _bytes = bytes.ToArray();

    public ReadOnlyMemory<byte> Bytes => _bytes.ToArray();

    public static InstallationSecret Create(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 32) throw new ArgumentException("Installation secret must contain 256 bits.", nameof(bytes));
        return new InstallationSecret(bytes.ToArray());
    }

    public override string ToString() => nameof(InstallationSecret);
}
