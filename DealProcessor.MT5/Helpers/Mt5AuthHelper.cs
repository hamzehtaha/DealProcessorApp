using System.Security.Cryptography;
using System.Text;

namespace DealProcessor.MT5.Helpers;

public static class Mt5AuthHelper
{
    public static string CalculatePasswordHash(string password)
    {
        var md5Password = Md5Bytes(Encoding.Unicode.GetBytes(password));
        var combined = Combine(md5Password, Encoding.UTF8.GetBytes("WebAPI"));
        var finalHash = Md5Bytes(combined);
        return ToHex(finalHash);
    }

    public static string CalculateSrvRandAnswer(string passwordHashHex, string srvRandHex)
    {
        var passwordHashBytes = HexToBytes(passwordHashHex);
        var srvRandBytes = HexToBytes(srvRandHex);

        var combined = Combine(passwordHashBytes, srvRandBytes);
        var final = Md5Bytes(combined);

        return ToHex(final);
    }

    public static string GenerateCliRand()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return ToHex(bytes);
    }

    private static byte[] Md5Bytes(byte[] input)
    {
        using var md5 = MD5.Create();
        return md5.ComputeHash(input);
    }

    private static byte[] Combine(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static string ToHex(byte[] bytes)
        => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}