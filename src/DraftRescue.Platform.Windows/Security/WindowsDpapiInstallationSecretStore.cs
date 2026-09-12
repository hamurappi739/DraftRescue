using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using DraftRescue.Application.Contracts.Persistence;

namespace DraftRescue.Platform.Windows.Security;

/// <summary>DPAPI-protected per-user installation HMAC secret with atomic first-create semantics.</summary>
public sealed class WindowsDpapiInstallationSecretStore : IInstallationSecretProvider
{
    private static readonly byte[] Magic = "DRS1"u8.ToArray();
    private const int SecretLength = 32;
    private const int HeaderLength = 10;
    private readonly string _path;

    public WindowsDpapiInstallationSecretStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public async Task<InstallationSecret> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await TryLoadAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null) return existing;

        var generated = RandomNumberGenerator.GetBytes(SecretLength);
        try
        {
            byte[] protectedKey;
            try
            {
                protectedKey = ProtectedData.Protect(generated, Array.Empty<byte>(), DataProtectionScope.CurrentUser);
            }
            catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException or UnauthorizedAccessException)
            {
                throw new DraftProtectionException(DraftProtectionFailureCode.DpapiFailure, ex);
            }

            var encoded = Encode(protectedKey);
            var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough | FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(encoded, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                File.Move(temporaryPath, _path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                var winner = await TryLoadAsync(cancellationToken).ConfigureAwait(false);
                if (winner is not null) return winner;
                throw new DraftProtectionException(DraftProtectionFailureCode.InstallationSecretUnavailable, ex);
            }
            finally
            {
                TryDeleteTemporary(temporaryPath);
                CryptographicOperations.ZeroMemory(encoded);
                CryptographicOperations.ZeroMemory(protectedKey);
            }

            return InstallationSecret.Create(generated);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(generated);
        }
    }

    private async Task<InstallationSecret?> TryLoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return null;
        byte[] encoded;
        try
        {
            encoded = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DraftProtectionException(DraftProtectionFailureCode.InstallationSecretUnavailable, ex);
        }

        try
        {
            var protectedKey = Decode(encoded);
            try
            {
                byte[] key;
                try
                {
                    key = ProtectedData.Unprotect(protectedKey, Array.Empty<byte>(), DataProtectionScope.CurrentUser);
                }
                catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException or UnauthorizedAccessException)
                {
                    throw new DraftProtectionException(DraftProtectionFailureCode.DpapiFailure, ex);
                }

                try { return InstallationSecret.Create(key); }
                catch (ArgumentException ex) { throw new DraftProtectionException(DraftProtectionFailureCode.InvalidInstallationSecret, ex); }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
            finally { CryptographicOperations.ZeroMemory(protectedKey); }
        }
        finally { CryptographicOperations.ZeroMemory(encoded); }
    }

    private static byte[] Encode(byte[] protectedKey)
    {
        if (protectedKey.Length == 0) throw new ArgumentException("Protected key is empty.", nameof(protectedKey));
        var result = new byte[HeaderLength + protectedKey.Length];
        Magic.CopyTo(result, 0);
        result[4] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(5), (uint)protectedKey.Length);
        result[9] = 0;
        protectedKey.CopyTo(result, HeaderLength);
        return result;
    }

    private static byte[] Decode(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length < HeaderLength || !encoded[..4].SequenceEqual(Magic) || encoded[4] != 1 || encoded[9] != 0)
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidInstallationSecret);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(encoded[5..]);
        if (length == 0 || length > int.MaxValue || encoded.Length != HeaderLength + (long)length)
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidInstallationSecret);
        return encoded.Slice(HeaderLength, (int)length).ToArray();
    }

    private static void TryDeleteTemporary(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // The temporary file contains only DPAPI ciphertext. Cleanup is best effort
            // and must never replace the typed operation result or expose exception text.
        }
        catch (UnauthorizedAccessException)
        {
            // The temporary file contains only DPAPI ciphertext. Cleanup is best effort
            // and must never replace the typed operation result or expose exception text.
        }
    }
}
