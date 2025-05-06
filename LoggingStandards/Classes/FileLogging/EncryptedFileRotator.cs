using CerbiClientLogging.Interfaces;
using CerbiStream.Interfaces;
using System;
using System.IO;

namespace CerbiStream.Classes.FileLogging
{
    public class EncryptedFileRotator
    {
        private readonly FileFallbackOptions _options;
        private readonly IEncryption _encryption;

        public EncryptedFileRotator(FileFallbackOptions options, IEncryption encryption)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));

            if (!_encryption.IsEnabled)
                throw new InvalidOperationException("Encryption must be enabled for file rotation.");
        }

        public void CheckAndRotateIfNeeded()
        {
            var fallbackPath = _options.FallbackFilePath;
            if (string.IsNullOrWhiteSpace(fallbackPath) || !File.Exists(fallbackPath))
                return;

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
                string rawLog = File.ReadAllText(fallbackPath);
                if (string.IsNullOrWhiteSpace(rawLog)) return;

                string encrypted = _encryption.Encrypt(rawLog);
                File.WriteAllText(archiveFileName, encrypted);
                File.WriteAllText(fallbackPath, string.Empty); // Clear current log file
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CerbiStream] Log rotation failed: {ex}");
            }
        }
    }
}
