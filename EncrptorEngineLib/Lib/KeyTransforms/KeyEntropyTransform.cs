using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class KeyEntropyTransform
    {
        public List<KeyEntropy> IntermediateEntropies { get; private set; } = new List<KeyEntropy>();

        public KeyEntropy TransformKeyEntropy(KeyEntropy input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            foreach (KeyEntropy entropy in IntermediateEntropies)
            {
                if (input.RawData.Length != entropy.RawData.Length)
                {
                    throw new Exception("All entropy shall have equal sizes!");
                }
            }

            KeyEntropy resultEntropy;

            //var cryptRandGen = System.Security.Cryptography.RandomNumberGenerator.Create();
            {
                //byte[] startIndexBytes = new byte[2];
                //cryptRandGen.GetBytes(startIndexBytes);

                //ushort startIndex = BitConverter.ToUInt16(startIndexBytes, 0);
                byte[] resultRawEntropy = new byte[input.RawData.Length];

                input.RawData.CopyTo(resultRawEntropy, 0);

                resultEntropy = new KeyEntropy(resultRawEntropy, input.StartPosition);
            }

            foreach (KeyEntropy entropy in IntermediateEntropies)
            {
                for (int i = 0; i < input.RawData.Length; i++)
                    resultEntropy.SetByteAtIndex(i, (byte)(resultEntropy.GetByteAtIndex(i) ^ entropy.GetByteAtIndex(i)));
            }

            return resultEntropy;
        }
    }
}
