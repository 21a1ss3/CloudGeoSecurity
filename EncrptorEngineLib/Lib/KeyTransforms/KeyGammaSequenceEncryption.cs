using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public class KeyGammaSequenceEncryption
    {
        public ISymmetricCipherTransform Cipher { get; set; }
        public byte[] EncryptionKey { get; set; }

        private NoPadding _noPadding = new NoPadding();

        public byte[][] Encrypt<T>(T keyGammaSequences, byte? version = null) where T : ICollection<KeyGammaSequence>, IList<KeyGammaSequence>
        {
            if ((keyGammaSequences == null))
                throw new ArgumentNullException(nameof(keyGammaSequences));

            if ((Cipher == null))
                throw new Exception($"{nameof(Cipher)} shall be specified first");

            byte[][] result = new byte[keyGammaSequences.Count][];

            InMemoryDataset srcKeys = new InMemoryDataset(result.Length);
            MemoryStream encryptedGammayStrem = new MemoryStream();

            Cipher.Encrypt = true;
            Cipher.Key = EncryptionKey;
            IConnectedTransform connEncr = Cipher.ConnectWithDataSet(srcKeys);

            connEncr.Result.RegisterReceiver(this, 1024, _noPadding);

            for (int i = 0; i < keyGammaSequences.Count; i++)
            {
                (byte[] openHeader, byte[] payload) encoded;

                if (version == null)
                    encoded = KeyGammaSequenceContainer.PutIn(keyGammaSequences[i]);
                else
                    encoded = KeyGammaSequenceContainer.PutIn(keyGammaSequences[i], version.Value);


                srcKeys.WriteToBuffer(i, encoded.payload);
                srcKeys.FinishItem(i);

                result[i] = encoded.openHeader;
            }

            while (connEncr.TransformNext()) ;

            for (int i = 0; i < result.Length; i++)
            {
                byte[] buffer;

                encryptedGammayStrem.Seek(0, SeekOrigin.Begin);
                encryptedGammayStrem.SetLength(0);

                encryptedGammayStrem.Write(result[i], 0, result[i].Length); //dumping open header

                while ((buffer = connEncr.Result.GetNextBlockForItem(this, i)) != null)
                    if (buffer.Length != 0)
                        encryptedGammayStrem.Write(buffer, 0, buffer.Length);

                encryptedGammayStrem.Flush();

                result[i] = encryptedGammayStrem.ToArray();
            }

            return result;
        }

        public KeyGammaSequence[] Decrypt(byte[][] encryptedKeyGammaSequences)
        {
            if ((encryptedKeyGammaSequences == null))
                throw new ArgumentNullException(nameof(encryptedKeyGammaSequences));

            if ((Cipher == null))
                throw new Exception($"{nameof(Cipher)} shall be specified first");

            KeyGammaSequence[] keyGammaSequences = new KeyGammaSequence[encryptedKeyGammaSequences.Length];
            byte[][] openHeaders = new byte[encryptedKeyGammaSequences.Length][];

            InMemoryDataset encryptedKeys = new InMemoryDataset(encryptedKeyGammaSequences.Length);
            MemoryStream clearedGammasStream = new MemoryStream();

            Cipher.Encrypt = false;
            Cipher.Key = EncryptionKey;
            IConnectedTransform connEncr = Cipher.ConnectWithDataSet(encryptedKeys);

            connEncr.Result.RegisterReceiver(this, 1024, _noPadding);

            for (int i = 0; i < encryptedKeyGammaSequences.Length; i++)
            {
                ArraySegment<byte> encrGammaSegmentation = new ArraySegment<byte>(encryptedKeyGammaSequences[i]);
                int openHeaderLen = KeyGammaSequenceContainer.GetOpenHeaderSize(encryptedKeyGammaSequences[i]);

                openHeaders[i] = encrGammaSegmentation.Slice(0, openHeaderLen).ToArray();

                encryptedKeys.WriteToBuffer(i, encrGammaSegmentation.Slice(openHeaderLen).ToArray());
                encryptedKeys.FinishItem(i);
            }

            while (connEncr.TransformNext()) ;

            for (int i = 0; i < encryptedKeyGammaSequences.Length; i++)
            {
                byte[] buffer;

                clearedGammasStream.Seek(0, SeekOrigin.Begin);
                clearedGammasStream.SetLength(0);

                while ((buffer = connEncr.Result.GetNextBlockForItem(this, i)) != null)
                    if (buffer.Length != 0)
                        clearedGammasStream.Write(buffer, 0, buffer.Length);

                clearedGammasStream.Flush();

                buffer = clearedGammasStream.ToArray();
                keyGammaSequences[i] = KeyGammaSequenceContainer.PullOut(openHeaders[i], buffer);
            }

            return keyGammaSequences;
        }
    }
}
