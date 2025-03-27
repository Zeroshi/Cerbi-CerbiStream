using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using System;

namespace CerbiStream.Extensions
{
    public static class CerbiStreamOptionExtensions
    {
        public static CerbiStreamOptions WithEncryptionFromConfiguration(
            this CerbiStreamOptions options,
            IConfiguration config)
        {
            var keyBase64 = config["Cerbi:Encryption:Key"];
            var ivBase64 = config["Cerbi:Encryption:IV"];

            if (!string.IsNullOrEmpty(keyBase64) && !string.IsNullOrEmpty(ivBase64))
            {
                var key = Convert.FromBase64String(keyBase64);
                var iv = Convert.FromBase64String(ivBase64);
                options.WithEncryptionKey(key, iv);
            }

            return options;
        }
    }

}
