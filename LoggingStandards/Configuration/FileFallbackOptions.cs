using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Configuration
{
    public class FileFallbackOptions
    {
        public bool Enable { get; set; }
        public string PrimaryFilePath { get; set; } = "logs/primary-log.txt";
        public string FallbackFilePath { get; set; } = "logs/fallback-log.txt";
        public int RetryCount { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 250;
    }

}
