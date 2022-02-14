using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class EncryptionEngineTransformCache
    {
        public ISequenceSet EncryptionSequenceSet { get; set; }
        public ISequenceSet DecryptSequenceSet { get; set; }
    }
}
