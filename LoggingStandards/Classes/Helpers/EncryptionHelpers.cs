using System;
using System.Security.Cryptography;
using System.Text;

namespace CerbiStream.Helpers
{
    public static class EncryptionHelpers
    {
        /// <summary>
        /// Generates a secure random 16-byte key for AES (128-bit).
        /// </summary>
        public static byte[] GenerateRandomKey() => RandomNumberGenerator.GetBytes(16);

        /// <summary>
        /// Generates a secure random 16-byte IV for AES.
        /// </summary>
        public static byte[] GenerateRandomIV() => RandomNumberGenerator.GetBytes(16);

        /// <summary>
        /// Encodes a byte array as a Base64 string.
        /// </summary>
        public static string ToBase64(byte[] input) => Convert.ToBase64String(input);

        /// <summary>
        /// Decodes a Base64 string to a byte array.
        /// </summary>
        public static byte[] FromBase64(string base64) => Convert.FromBase64String(base64);

        /// <summary>
        /// Generates a debug key/IV with a warning. Intended for development only.
        /// </summary>
        public static (byte[] Key, byte[] IV) GetInsecureDefaultKeyPair()
        {
            const string keyStr = "CerbiDefaultKey1";  // ✅ 16 chars
            const string ivStr = "CerbiDefaultIV__";  // ✅ 16 chars

            var key = Encoding.ASCII.GetBytes(keyStr);
            var iv = Encoding.ASCII.GetBytes(ivStr);

            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("Fallback key/IV must be exactly 16 bytes for AES-128.");

            return (key, iv);
        }


    }
}
