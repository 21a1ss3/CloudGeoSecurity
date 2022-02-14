using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Encryptor.Lib
{
    public class EncryptionSet
    {
        private EncryptionSet()
        {

        }

        public EncryptionSet(Guid setId, Guid keyChain, EncryptionSetItem[] encryptions)
        {
            if (Guid.Empty == setId)
                throw new ArgumentException($"{nameof(setId)} could not be equal to Guid.Empty");

            if (Guid.Empty == keyChain)
                throw new ArgumentException($"{nameof(keyChain)} could not be equal to Guid.Empty");

            if (encryptions == null)
                throw new ArgumentNullException(nameof(encryptions));

            foreach (var encryption in encryptions)
                if (encryption == null)
                    throw new ArgumentException($"Encryption in {nameof(encryptions)} cannot be null");


            SetId = setId;
            KeyChain = keyChain;
            Encryptions = new ReadOnlyCollection<EncryptionSetItem>(encryptions);
        }

        public ReadOnlyCollection<EncryptionSetItem> Encryptions { get; private set; }
        public Guid SetId { get; private set; }
        public Guid KeyChain { get; private set; }


    }
}
