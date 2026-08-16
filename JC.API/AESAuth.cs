using System;
using System.Security.Cryptography;
using System.Text;

public static class AESAuth
{
    // Simple symmetric key for demo purposes; in real app use env var
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("abcdef9876543210"); // 16 bytes

    public static string EncryptToken(string username, string password)
    {
        var payload = $"{username}:{password}";
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(payload);
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipher);
    }

    public static (string username, string password)? DecryptToken(string token)
    {
        try
        {
            var cipher = Convert.FromBase64String(token);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            var payload = Encoding.UTF8.GetString(plain);
            var parts = payload.Split(":", 2);
            if (parts.Length != 2) return null;
            return (parts[0], parts[1]);
        }
        catch
        {
            return null;
        }
    }
}
