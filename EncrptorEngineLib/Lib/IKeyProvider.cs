using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public interface IKeyProvider
    {
        public void Require(int index, int size);
        public byte[] GetKey(int index);
    }
}
