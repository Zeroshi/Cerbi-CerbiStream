using CerbiClientLogging.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class AesEncryption : IEncryption
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public bool IsEnabled { get; }
    public EncryptionType EncryptionMethod => EncryptionType.AES;

    public AesEncryption(byte[] key, byte[] iv, bool enable = true)
    {
        _key = key;
        _iv = iv;
        IsEnabled = enable;
    }

    public string Encrypt(string data)
    {
        if (!IsEnabled) return data;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(data);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string encrypted)
    {
        if (!IsEnabled) return encrypted;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var buffer = Convert.FromBase64String(encrypted);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }
}
