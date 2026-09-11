using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace DraftRescue.Platform.Windows.Security;

/// <summary>Strict, content-free envelope codec used immediately before/after DPAPI.</summary>
public static class DraftPayloadV1Codec
{
    private static readonly byte[] Magic = "DRP1"u8.ToArray();
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[] Encode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var textBytes = StrictUtf8.GetBytes(text);
        try
        {
            if ((ulong)textBytes.Length > uint.MaxValue) throw new ArgumentException("Payload is too large.", nameof(text));
            var result = new byte[12 + textBytes.Length];
            Magic.CopyTo(result, 0);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(4), 1);
            result[6] = 1;
            result[7] = 0;
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), (uint)textBytes.Length);
            textBytes.CopyTo(result, 12);
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(textBytes);
        }
    }

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12 || !bytes[..4].SequenceEqual(Magic))
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectedPayload);
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]) != 1 || bytes[6] != 1 || bytes[7] != 0)
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectedPayload);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]);
        if (length > int.MaxValue || bytes.Length != 12L + length)
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectedPayload);
        try
        {
            return StrictUtf8.GetString(bytes.Slice(12, (int)length));
        }
        catch (DecoderFallbackException ex)
        {
            throw new DraftProtectionException(DraftProtectionFailureCode.InvalidProtectedPayload, ex);
        }
    }
}
