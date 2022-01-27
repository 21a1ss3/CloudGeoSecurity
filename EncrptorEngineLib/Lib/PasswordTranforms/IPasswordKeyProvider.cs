using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public interface IPasswordKeyProvider
    {
        public byte[] GetEncryptionKey(string password, int keySize);
    }
}
