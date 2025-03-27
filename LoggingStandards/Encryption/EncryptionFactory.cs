using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Helpers;
using CerbiStream.Logging.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiStream.Encryption
{
    public static class EncryptionFactory
    {
        public static IEncryption GetEncryption(CerbiStreamOptions options)
        {
            return options.EncryptionMode switch
            {
                EncryptionType.None => new NoOpEncryption(),
                EncryptionType.Base64 => new EncryptionImplementation(true, EncryptionType.Base64),
                EncryptionType.AES => CreateAesEncryption(options),
                _ => new NoOpEncryption()
            };
        }

        private static IEncryption CreateAesEncryption(CerbiStreamOptions options)
        {
            var key = options.EncryptionKey;
            var iv = options.EncryptionIV;

            if (key == null || iv == null)
            {
                // ✅ Force valid 16-byte fallback values
                (key, iv) = EncryptionHelpers.GetInsecureDefaultKeyPair();
            }

            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("AES encryption requires 16-byte key and IV for AES-128.");

            return new AesEncryption(key, iv);
        }


    }

}
