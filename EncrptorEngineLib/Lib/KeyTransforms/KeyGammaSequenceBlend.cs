using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class KeyGammaSequenceBlend
    {
        public List<KeyGammaSequence> IntermediateGammaSequneties { get; private set; } = new List<KeyGammaSequence>();

        public KeyGammaSequence TransformKeyGammaSequence(KeyGammaSequence input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            foreach (KeyGammaSequence keyGammaSequence in IntermediateGammaSequneties)
            {
                if (input.RawData.Length != keyGammaSequence.RawData.Length)
                {
                    throw new Exception("All key gamma sequences shall have equal sizes!");
                }
            }

            KeyGammaSequence resultKeyGammaSequence;

            {
                byte[] resultRawGammaSeq = new byte[input.RawData.Length];

                input.RawData.CopyTo(resultRawGammaSeq, 0);

                resultKeyGammaSequence = new KeyGammaSequence(resultRawGammaSeq, input.StartPosition);
            }

            foreach (KeyGammaSequence keyGammaSequence in IntermediateGammaSequneties)
            {
                for (int i = 0; i < input.RawData.Length; i++)
                    resultKeyGammaSequence.SetByteAtIndex(i, (byte)(resultKeyGammaSequence.GetByteAtIndex(i) ^ keyGammaSequence.GetByteAtIndex(i)));
            }

            return resultKeyGammaSequence;
        }
    }
}
