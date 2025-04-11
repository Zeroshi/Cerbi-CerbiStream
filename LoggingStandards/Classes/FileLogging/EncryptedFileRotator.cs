using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CerbiStream.Classes.FileLogging
{
    //todo: refactor to use the same aes that was created prior
    public class EncryptedFileRotator
    {
        private readonly FileFallbackOptions _options;

        public EncryptedFileRotator(FileFallbackOptions options)
        {
            _options = options;
        }

        public void CheckAndRotateIfNeeded()
        {
            var fallbackPath = _options.FallbackFilePath;
            if (!File.Exists(fallbackPath)) return;

            var fileInfo = new FileInfo(fallbackPath);
            var now = DateTime.UtcNow;
            var age = now - fileInfo.CreationTimeUtc;

            bool rotateBySize = _options.MaxFileSizeBytes > 0 && fileInfo.Length >= _options.MaxFileSizeBytes;
            bool rotateByAge = _options.MaxFileAge > TimeSpan.Zero && age >= _options.MaxFileAge;

            if (!rotateBySize && !rotateByAge) return;

            string archiveFileName = Path.Combine(
                Path.GetDirectoryName(fallbackPath)!,
                Path.GetFileNameWithoutExtension(fallbackPath) + "-" + now.ToString("yyyyMMddHHmmss") + ".enc"
            );

            try
            {
                EncryptAndArchiveFile(fallbackPath, archiveFileName);
                File.WriteAllText(fallbackPath, string.Empty); // Clear current log file
            }
            catch (Exception ex)
            {
                // You might want to log this to another fallback or monitoring tool
                Console.Error.WriteLine("Log rotation failed: " + ex);
            }
        }

        private void EncryptAndArchiveFile(string sourcePath, string destinationPath)
        {
            var key = Encoding.UTF8.GetBytes(_options.EncryptionKey ?? "default-32-char-test-key!!");
            var iv = Encoding.UTF8.GetBytes(_options.EncryptionIV ?? "default-16byte-iv");

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;

            using var encryptor = aes.CreateEncryptor();
            using var inputStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
            inputStream.CopyTo(cryptoStream);
        }
    }
}
