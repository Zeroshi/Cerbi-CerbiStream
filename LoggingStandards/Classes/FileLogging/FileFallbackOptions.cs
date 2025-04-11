using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Classes.FileLogging
{
    public class FileFallbackOptions
    {
        public bool Enable { get; set; } = false;
        public string PrimaryFilePath { get; set; } = "logs/log-primary.json";
        public string FallbackFilePath { get; set; } = "logs/log-fallback.json";
        public int RetryCount { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        //encryption options
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB default
        public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromMinutes(10); // Default age
        public string? EncryptionKey { get; set; }
        public string? EncryptionIV { get; set; }
    }
}
