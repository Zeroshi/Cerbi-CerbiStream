using CerbiClientLogging.Interfaces;
using CerbiStream.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiClientLogging.Implementations
{
    public class EncryptionImplementation : IEncryption
    {
        public bool IsEnabled { get; private set; }
        public EncryptionType EncryptionMethod { get; private set; }

        public EncryptionImplementation(bool enableEncryption = true, EncryptionType method = EncryptionType.Base64)
        {
            IsEnabled = enableEncryption;
            EncryptionMethod = method;
        }

        public string Encrypt(string data)
        {
            if (!IsEnabled) return data;

            return EncryptionMethod switch
            {
                EncryptionType.Base64 => Convert.ToBase64String(Encoding.UTF8.GetBytes(data)),
                EncryptionType.AES => EncryptAes(data),
                _ => data
            };
        }

        public string Decrypt(string data)
        {
            if (!IsEnabled) return data;

            return EncryptionMethod switch
            {
                EncryptionType.Base64 => Encoding.UTF8.GetString(Convert.FromBase64String(data)),
                EncryptionType.AES => DecryptAes(data),
                _ => data
            };
        }

        private string EncryptAes(string plainText)
        {
            byte[] key = Encoding.UTF8.GetBytes("1234567890123456");
            byte[] iv = Encoding.UTF8.GetBytes("1234567890123456");

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cs);
            writer.Write(plainText);
            writer.Flush();  // ✅ Ensure everything is flushed
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }

        private string DecryptAes(string encryptedText)
        {
            byte[] key = Encoding.UTF8.GetBytes("1234567890123456");
            byte[] iv = Encoding.UTF8.GetBytes("1234567890123456");
            byte[] buffer = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            return reader.ReadToEnd();
        }
    }
}
