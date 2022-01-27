using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public static class KeyEntropyContainer
    {
        private enum ContainerVersion : byte
        {
            V0 = 0,
        }

        public static byte[] PutIn(KeyEntropy keyEntropy, byte version = 0)
        {
            switch ((ContainerVersion)version)
            {
                case ContainerVersion.V0:
                    return _putInV0(keyEntropy);


                default:
                    throw new Exception("Unsupported version of Key Entropy Container");
            }
        }

        private static byte[] _putInV0(KeyEntropy keyEntropy)
        {
            if (keyEntropy == null)
                throw new ArgumentNullException(nameof(keyEntropy));
            byte[] result;

            using (MemoryStream rawStream = new MemoryStream())
            {
                using (BinaryWriter rawWriter = new BinaryWriter(rawStream))
                {

                    rawWriter.Write((byte)ContainerVersion.V0);
                    rawWriter.Write(keyEntropy.StartPosition);
                    rawWriter.Write(keyEntropy.RawData);

                    rawWriter.Flush();

                    result = rawStream.ToArray();

                    rawWriter.Close();
                }
            }

            return result;
        }

        public static KeyEntropy PullOut(byte[] raw)
        {
            using (MemoryStream rawStream = new MemoryStream(raw))
            {
                using (BinaryReader rawReader = new BinaryReader(rawStream))
                {
                    ContainerVersion version = (ContainerVersion)rawReader.ReadByte();

                    switch (version)
                    {
                        case ContainerVersion.V0:
                            return _pullOutV0(raw);


                        default:
                            throw new Exception("Unsupported version of Key Entropy Container");
                    }
                }
            }
        }        

        private static KeyEntropy _pullOutV0(byte[] raw)
        {
            using (MemoryStream rawStream = new MemoryStream(raw))
            {
                using (BinaryReader rawReader = new BinaryReader(rawStream))
                {
                    //ignoring the version
                    rawReader.ReadByte();

                    ushort startPos = rawReader.ReadUInt16();

                    /*
                        Because BinaryReader does not have ReadBytesToEnd function
                        and we are using MemoryStream 
                        we can create the buffere with original raw data size
                        and ask the Read function to fill
                        But it will actually fill it only up to EOF position
                        and then we would be able to shrink our buffer
                    */
                    byte[] entropyBody = new byte[raw.Length];

                    int readed = rawReader.Read(entropyBody, 0, entropyBody.Length);
                    Array.Resize(ref entropyBody, readed);

                    rawReader.Close();

                    return new KeyEntropy(entropyBody, startPos);
                }
            }

                }
    }
}
