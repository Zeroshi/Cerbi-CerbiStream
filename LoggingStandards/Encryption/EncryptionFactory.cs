using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
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
                EncryptionType.AES => new EncryptionImplementation(true, EncryptionType.AES),
                _ => new EncryptionImplementation(false)
            };
        }
    }

}
