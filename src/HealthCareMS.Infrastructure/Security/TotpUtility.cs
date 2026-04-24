using System.Security.Cryptography;
using System.Text;

namespace HealthCareMS.Infrastructure.Security;

internal static class TotpUtility
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string GenerateSecret(int byteLength = 20)
    {
        return Base32Encode(RandomNumberGenerator.GetBytes(byteLength));
    }

    public static string BuildOtpAuthUri(string issuer, string email, string secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
    }

    public static bool ValidateCode(string secret, string code, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var timestamp = now ?? DateTimeOffset.UtcNow;
        for (var offset = -1; offset <= 1; offset++)
        {
            if (string.Equals(GenerateCode(secret, timestamp.AddSeconds(offset * 30)), normalizedCode, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateCode(string secret, DateTimeOffset time)
    {
        var secretBytes = Base32Decode(secret);
        var timestep = time.ToUnixTimeSeconds() / 30;
        var counter = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        return (binaryCode % 1_000_000).ToString("000000");
    }

    private static string Base32Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var output = new StringBuilder((data.Length + 7) * 8 / 5);
        var bitBuffer = (int)data[0];
        var next = 1;
        var bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    bitBuffer <<= 8;
                    bitBuffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    var pad = 5 - bitsLeft;
                    bitBuffer <<= pad;
                    bitsLeft += pad;
                }
            }

            var index = 0x1F & (bitBuffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            output.Append(Base32Alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var cleaned = input.Trim().TrimEnd('=').ToUpperInvariant();
        if (cleaned.Length == 0)
        {
            return [];
        }

        var output = new List<byte>(cleaned.Length * 5 / 8);
        var bitBuffer = 0;
        var bitsLeft = 0;

        foreach (var ch in cleaned)
        {
            var value = Base32Alphabet.IndexOf(ch);
            if (value < 0)
            {
                continue;
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((bitBuffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return [.. output];
    }
}
