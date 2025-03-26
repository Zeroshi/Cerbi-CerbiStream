using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Interfaces
{
    public interface IEncryptionTypeProvider
    {
        public enum EncryptionType
        {
            None,
            Base64,
            AES
        }

    }
}
