using System.Security.Cryptography;
using System.Text;

namespace ApiGrupos.Services;

public static class SecretHash
{
    public static string ComputeSha256(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool IsSha256Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 64 && trimmed.All(Uri.IsHexDigit);
    }

    public static bool VerifySha256(string secret, string expectedHash)
    {
        if (string.IsNullOrEmpty(secret) || !IsSha256Hash(expectedHash))
        {
            return false;
        }

        var actualHash = ComputeSha256(secret);
        var actualBytes = Encoding.ASCII.GetBytes(actualHash);
        var expectedBytes = Encoding.ASCII.GetBytes(expectedHash.Trim().ToLowerInvariant());

        return actualBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
