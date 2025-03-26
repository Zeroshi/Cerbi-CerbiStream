using CerbiClientLogging.Interfaces;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiClientLogging.Implementations
{
    public class NoOpEncryption : IEncryption
    {
        public bool IsEnabled => false;
        public EncryptionType EncryptionMethod => EncryptionType.None;
        public string Encrypt(string input) => input;
        public string Decrypt(string input) => input;
    }
}
