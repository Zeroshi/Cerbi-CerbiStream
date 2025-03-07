using CerbiClientLogging.Interfaces;
using System;

namespace CerbiClientLogging.Implementations
{
    public class EncryptionImplementation : IEncryption
    {
        public bool IsEnabled { get; private set; }

        public EncryptionImplementation(bool enableEncryption = true)
        {
            IsEnabled = enableEncryption;
        }

        public string Encrypt(string data)
        {
            return IsEnabled ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data)) : data;
        }

        public string Decrypt(string encryptedData)
        {
            return IsEnabled ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedData)) : encryptedData;
        }
    }
}
