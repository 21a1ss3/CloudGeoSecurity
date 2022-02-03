using Encryptor.Lib;
using System;
using System.IO;

namespace CloudAppDecryptor
{
    class ProgramDecrypt
    {
        static void Main(string[] args)
        {
            int clouds = 3;
            int encryptionLayers = 2;


            SequenceSet encrSet = new SequenceSet();

            RandomPadding rndPadding = new RandomPadding();
            NoPadding noPad = new NoPadding();

            KeyGammaSequence[] keyGammSequences = new KeyGammaSequence[encryptionLayers];
            KeyGammaSequenceEncryption keyGammaSeqEncr = new KeyGammaSequenceEncryption();
            KeyGammaSequenceBlend encrGammaSequnecesBlend = new KeyGammaSequenceBlend();
            byte[] salt = new byte[64];


            Console.Write("Proide master password: ");
            string password = Console.ReadLine();
            
            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                System.Text.Encoding.UTF8.GetBytes(password),
                salt, 2048, System.Security.Cryptography.HashAlgorithmName.SHA256);

            keyGammaSeqEncr.EncryptionKey = pbkdf2.GetBytes(32);
            
            {
                AesCbcEncryptionTransfrom keyTransform = new AesCbcEncryptionTransfrom();
                keyTransform.Padding = rndPadding;
                keyGammaSeqEncr.Cipher = keyTransform;
            }


            for (int i = 0; i < clouds; i++)
            {
                encrGammaSequnecesBlend.IntermediateGammaSequneties.Add(
                                                    _decryptGammaSeq(
                                                               pbkdf2,
                                                               keyGammaSeqEncr,
                                                               i,
                                                               0
                                                               ));
            }

            for (int i = 0; i < encryptionLayers; i++)
            {
                keyGammSequences[i] = _decryptGammaSeq(
                                                pbkdf2,
                                                keyGammaSeqEncr,
                                                clouds - i % clouds - 1,
                                                i / clouds + 1
                                           );

                keyGammSequences[i] = encrGammaSequnecesBlend.TransformKeyGammaSequence(keyGammSequences[i]);
            }



            for (int i = keyGammSequences.Length - 1; i > -1; i--)
            {
                AesCbcEncryptionTransfrom transform = new AesCbcEncryptionTransfrom();
                
                transform.Key = keyGammSequences[i].ExtractBytes(0, 32);
                transform.Encrypt = false;
                transform.Padding = rndPadding;

                encrSet.ScheduleTransformation(transform);

                /*ClearPadTransform clearPad = new ClearPadTransform();
                clearPad.Padding = rndPadding;
                clearPad.BlockSize = 16;
                encrSet.ScheduleTransformation(clearPad);*/
            }


            FileDataset inputFileDS = new FileDataset();
            inputFileDS.AddFile("cloud0\\encrypted.bin");
            inputFileDS.FixFileset();


            IConnectedTransform transformation = encrSet.StartTransformation(inputFileDS);
            object receiver = new object();
            transformation.Result.RegisterReceiver(receiver, 1024, noPad);

            FileStream stream = File.Open("decrypted.txt", FileMode.Create, FileAccess.Write);

            byte lastLoop = 1;

            while (transformation.TransformNext() || (lastLoop-- > 0))
            {
                byte[] rawData;
                while (((rawData = transformation.Result.GetNextBlockForItem(receiver, 0)) != null) && (rawData.Length > 0))
                {
                    stream.Write(rawData, 0, rawData.Length);
                }

                stream.Flush();
            }

            stream.Close();
        }
        
        private static KeyGammaSequence _decryptGammaSeq(System.Security.Cryptography.Rfc2898DeriveBytes pbkdf2,
                                        KeyGammaSequenceEncryption keyGammaSeqEncr,
                                        int iteration,
                                        int set)
        {
            byte[] salt = new byte[64];
            byte[] rawGammaSequence;

            using (FileStream keyStream = File.Open($"cloud{iteration}\\key{iteration}_{set}.bin", FileMode.Open, FileAccess.Read))
            {
                int readBytes = 0;
                rawGammaSequence = new byte[keyStream.Length - salt.Length];

                do
                    readBytes += keyStream.Read(salt, readBytes, salt.Length - readBytes);
                while (readBytes < salt.Length);

                readBytes = 0;


                do
                    readBytes += keyStream.Read(rawGammaSequence, readBytes, rawGammaSequence.Length - readBytes);
                while (readBytes < rawGammaSequence.Length);

                keyStream.Close();
            }

            pbkdf2.Salt = salt;
            keyGammaSeqEncr.EncryptionKey = pbkdf2.GetBytes(32);

            KeyGammaSequence[] decrypted = keyGammaSeqEncr.Decrypt(new byte[][] { rawGammaSequence });

            return decrypted[0];
        }
    }
}
