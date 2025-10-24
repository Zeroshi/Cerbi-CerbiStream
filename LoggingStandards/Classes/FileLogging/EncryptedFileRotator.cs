using CerbiClientLogging.Interfaces;
using CerbiStream.Interfaces;
using System;
using System.IO;

namespace CerbiStream.Classes.FileLogging
{
    /// <summary>
    /// The EncryptedFileRotator class is responsible for managing file rotation of log files
    /// while ensuring their content is encrypted and securely archived. It evaluates conditions
    /// such as file size and age to decide when a log file should be rotated, creating a new
    /// encrypted archive and clearing the current log file when necessary.
    /// </summary>
    /// <remarks>
    /// This class works in conjunction with an encryption mechanism and file fallback options
    /// to ensure that log files comply with size and age restrictions, while also preserving
    /// security by encrypting archived files. It requires valid instances of both
    /// <see cref="FileFallbackOptions"/> and <see cref="IEncryption"/> to function properly.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the options or encryption instances passed to the constructor are null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if encryption functionality is not enabled.
    /// </exception>
    public class EncryptedFileRotator
    {
        /// <summary>
        /// Represents the configuration options for file fallback operations in the EncryptedFileRotator.
        /// </summary>
        /// <remarks>
        /// The <c>_options</c> field contains properties such as file paths, retry settings, and
        /// specifications for maximum file size and age that determine when log rotation should occur.
        /// These options are critical for managing file-based logging and ensuring that logs are archived
        /// and maintained securely with encryption.
        /// </remarks>
        private readonly FileFallbackOptions _options;

        /// <summary>
        /// Represents the encryption service used to encrypt and decrypt data during log file rotation.
        /// </summary>
        /// <remarks>
        /// The encryption service implements the <see cref="IEncryption"/> interface and must be enabled.
        /// It is responsible for securely encrypting log data before rotation and decrypting when needed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the encryption service is not enabled during initialization.
        /// </exception>
        private readonly IEncryption _encryption;

        /// Represents a class responsible for handling the rotation of encrypted log files.
        /// Ensures that the log files are rotated based on size or age criteria and encrypts the archived files.
        public EncryptedFileRotator(FileFallbackOptions options, IEncryption encryption)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));

            if (!_encryption.IsEnabled)
                throw new InvalidOperationException("Encryption must be enabled for file rotation.");
        }

        /// Checks the current fallback log file and rotates it if certain conditions are met.
        /// Log rotation occurs when the file size exceeds the maximum limit or the file age
        /// exceeds the specified threshold. During rotation, the existing log file is encrypted
        /// and archived, then the current log file is cleared.
        /// The method performs the following:
        /// - Validates if the fallback log file path exists.
        /// - Checks if the file meets the rotation conditions based on size and age.
        /// - Encrypts the file content using the provided encryption implementation.
        /// - Writes the encrypted content to a new archived file with a timestamped name.
        /// - Clears the contents of the original fallback file.
        /// If any exceptions occur during the rotation process, the error details are logged to
        /// the console, and the operation is silently handled without disrupting subsequent calls.
        /// Preconditions:
        /// - A fallback log file path must be defined in the `FileFallbackOptions`.
        /// - The encryption mechanism must be implemented and provided via the `IEncryption` instance.
        /// Rotation Triggers:
        /// - Maximum allowed file size, as defined in `FileFallbackOptions.MaxFileSizeBytes`.
        /// - Maximum allowed file age, as defined in `FileFallbackOptions.MaxFileAge`.
        /// Note: If the fallback file is empty or contains only whitespace, rotation is skipped.
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
