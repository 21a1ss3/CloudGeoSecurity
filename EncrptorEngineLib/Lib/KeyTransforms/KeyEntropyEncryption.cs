using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public class KeyEntropyEncryption
    {
        public ISymmetricCipherTransform Cipher { get; set; }
        public byte[] EncryptionKey { get; set; }

        private NoPadding _noPadding = new NoPadding();

        public byte[][] Encrypt<T>(T entropies, byte? version = null) where T : ICollection<KeyEntropy>, IList<KeyEntropy>
        {
            if ((entropies == null))
                throw new ArgumentNullException(nameof(entropies));

            if ((Cipher == null))
                throw new Exception($"{nameof(Cipher)} shall be specified first");

            byte[][] result = new byte[entropies.Count][];

            for (int i = 0; i < entropies.Count; i++)
            {
                byte[] encoded;

                if (version == null)
                    encoded = KeyEntropyContainer.PutIn(entropies[i]);
                else
                    encoded = KeyEntropyContainer.PutIn(entropies[i], version.Value);

                result[i] = encoded;
            }

            InMemoryDataset srcKeys = new InMemoryDataset(result.Length);
            MemoryStream encryptedEntropyStrem = new MemoryStream();

            Cipher.Encrypt = true;
            Cipher.Key = EncryptionKey;
            IConnectedTransform connEncr = Cipher.ConnectWithDataSet(srcKeys);

            connEncr.Result.RegisterReceiver(this, 1024, _noPadding);

            for (int i = 0; i < result.Length; i++)
            {
                srcKeys.WriteToBuffer(i, result[i]);
                srcKeys.FinishItem(i);
            }

            while (connEncr.TransformNext()) ;

            for (int i = 0; i < result.Length; i++)
            {
                byte[] buffer;

                encryptedEntropyStrem.Seek(0, SeekOrigin.Begin);
                encryptedEntropyStrem.SetLength(0);

                while ((buffer = connEncr.Result.GetNextBlockForItem(this, i)) != null)
                    if (buffer.Length != 0)
                        encryptedEntropyStrem.Write(buffer, 0, buffer.Length);

                encryptedEntropyStrem.Flush();

                result[i] = encryptedEntropyStrem.ToArray();
            }

            return result;
        }

        public KeyEntropy[] Decrypt(byte[][] encryptedEntropies)
        {
            if ((encryptedEntropies == null))
                throw new ArgumentNullException(nameof(encryptedEntropies));

            if ((Cipher == null))
                throw new Exception($"{nameof(Cipher)} shall be specified first");

            KeyEntropy[] entropies = new KeyEntropy[encryptedEntropies.Length];

            InMemoryDataset encryptedKeys = new InMemoryDataset(encryptedEntropies.Length);
            MemoryStream clearedEntropyStream = new MemoryStream();

            Cipher.Encrypt = false;
            Cipher.Key = EncryptionKey;
            IConnectedTransform connEncr = Cipher.ConnectWithDataSet(encryptedKeys);

            connEncr.Result.RegisterReceiver(this, 1024, _noPadding);

            for (int i = 0; i < encryptedEntropies.Length; i++)
            {
                encryptedKeys.WriteToBuffer(i, encryptedEntropies[i]);
                encryptedKeys.FinishItem(i);
            }

            while (connEncr.TransformNext()) ;

            for (int i = 0; i < encryptedEntropies.Length; i++)
            {
                byte[] buffer;

                clearedEntropyStream.Seek(0, SeekOrigin.Begin);
                clearedEntropyStream.SetLength(0);

                while ((buffer = connEncr.Result.GetNextBlockForItem(this, i)) != null)
                    if (buffer.Length != 0)
                        clearedEntropyStream.Write(buffer, 0, buffer.Length);

                clearedEntropyStream.Flush();

                buffer = clearedEntropyStream.ToArray();
                entropies[i] = KeyEntropyContainer.PullOut(buffer);
            }

            return entropies;
        }
    }
}
