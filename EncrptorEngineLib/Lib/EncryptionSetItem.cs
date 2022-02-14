using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class EncryptionSetItem
    {
        private EncryptionSetItem()
        {

        }

        public EncryptionSetItem(string encryptionName, string paddingName, int keySize, Guid keyId)
        {
            if (string.IsNullOrWhiteSpace(encryptionName))
                throw new ArgumentNullException(nameof(encryptionName));

            if (string.IsNullOrWhiteSpace(paddingName))
                throw new ArgumentNullException(nameof(paddingName));

            if (keySize < 0)
                throw new ArgumentOutOfRangeException(nameof(keySize));

            if (Guid.Empty == keyId)
                throw new ArgumentException($"{nameof(keyId)} could not be equal to Guid.Empty");

            EncryptionName = encryptionName;
            PaddingName = paddingName;
            KeySize = keySize;
            KeyId = keyId;
        }

        public string EncryptionName { get; private set; }
        public string PaddingName { get; private set; }
        public int KeySize { get; private set; }
        public Guid KeyId { get; private set; }
    }
}
