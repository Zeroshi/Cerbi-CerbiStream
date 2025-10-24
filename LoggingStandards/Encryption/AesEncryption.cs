using CerbiClientLogging.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

/// <summary>
/// Provides AES encryption and decryption functionalities using a specified key and initialization vector (IV).
/// Implements the <see cref="CerbiClientLogging.Interfaces.IEncryption"/> interface.
/// </summary>
public class AesEncryption : IEncryption
{
    /// <summary>
    /// Represents the cryptographic key used for AES encryption and decryption processes.
    /// The key is essential for ensuring secure transformation of data and should match
    /// the key used during both encryption and decryption operations.
    /// </summary>
    private readonly byte[] _key;

    /// <summary>
    /// Represents the initialization vector (IV) used for AES encryption and decryption operations.
    /// The IV is a random or unique value required for certain modes of encryption to ensure security.
    /// </summary>
    private readonly byte[] _iv;

    /// Indicates whether the encryption mechanism is enabled or disabled.
    /// This property is useful to determine if the encryption and decryption
    /// operations for the current instance of the encryption class are active.
    /// When `true`, encryption and decryption operations are performed as expected.
    /// When `false`, the encryption and decryption methods will return the input
    /// data without performing any transformations.
    /// This property is set upon instantiation of the encryption class and cannot
    /// be modified afterward.
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the encryption method used by the implementation.
    /// </summary>
    /// <remarks>
    /// This property indicates the specific encryption type applied in the encryption process.
    /// The value is determined by the enum <see cref="IEncryptionTypeProvider.EncryptionType"/>
    /// and typically specifies a concrete encryption algorithm (e.g., AES, Base64, None).
    /// </remarks>
    public EncryptionType EncryptionMethod => EncryptionType.AES;

    /// Represents an Advanced Encryption Standard (AES) encryption and decryption utility.
    /// Implements the IEncryption interface, providing methods for encrypting and decrypting
    /// strings using AES with a specified key and initialization vector (IV).
    public AesEncryption(byte[] key, byte[] iv, bool enable = true)
    {
        _key = key;
        _iv = iv;
        IsEnabled = enable;
    }

    /// <summary>
    /// Encrypts the given plain text data using AES encryption.
    /// </summary>
    /// <param name="data">The plain text data to be encrypted.</param>
    /// <returns>The encrypted data as a Base64 encoded string. If encryption is disabled, returns the original data.</returns>
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

    /// Decrypts the provided encrypted string using the AES algorithm, provided encryption is enabled.
    /// <param name="encrypted">The encrypted string to be decrypted. Expected to be a base64-encoded string.</param>
    /// <returns>The original decrypted string. If encryption is disabled, the input string is returned as is.</returns>
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
