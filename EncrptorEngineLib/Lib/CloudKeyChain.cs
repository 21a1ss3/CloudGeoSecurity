using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class CloudKeyChain
    {
        public Guid KeyChainId { get; set; }

        public List<Guid> Keys { get; private set; } = new List<Guid>();
    }
}
