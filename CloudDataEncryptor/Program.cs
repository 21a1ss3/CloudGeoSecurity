using Encryptor.Lib;
using System;
using System.IO;
using System.Linq;

namespace CloudDataEncryptor
{
    class Program
    {
        static System.Security.Cryptography.RandomNumberGenerator _generator = System.Security.Cryptography.RandomNumberGenerator.Create();
        static RandomPadding _rndPadding = new RandomPadding();
        static NoPadding _noPad = new NoPadding();

        static void Main(string[] args)
        {
            int clouds = 3;
            int encryptionLayers = 2;


            object receiver = new object();
            SequenceSet fileEncrSet = new SequenceSet();

            KeyGammaSequence[] openEncryptionKeyGammaSequnces = new KeyGammaSequence[encryptionLayers];
            AesCbcEncryptionTransfrom keyGammaSequenceEncryptionTransform = new AesCbcEncryptionTransfrom();

            KeyGammaSequenceBlend encrGammaSequnecesBlend = new KeyGammaSequenceBlend();
            KeyGammaSequenceEncryption keyGammaSeqEncr = new KeyGammaSequenceEncryption();


            keyGammaSequenceEncryptionTransform.Padding = _rndPadding;
            keyGammaSeqEncr.Cipher = keyGammaSequenceEncryptionTransform;



            for (int i = 0; i < encryptionLayers; i++)
            {
                openEncryptionKeyGammaSequnces[i] = KeyGammaSequence.CreateRandom(512);

                AesCbcEncryptionTransfrom transform = new AesCbcEncryptionTransfrom();

                transform.Key = openEncryptionKeyGammaSequnces[i].ExtractBytes(0, 32);
                transform.Encrypt = true;
                transform.Padding = _rndPadding;

                fileEncrSet.ScheduleTransformation(transform);
            }



            Console.Write("Proide filename to encrypt: ");
            string filename = Console.ReadLine();

            Console.Write("Proide master password: ");
            string password = Console.ReadLine();


            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                System.Text.Encoding.UTF8.GetBytes(password),
                new byte[64], 2048, System.Security.Cryptography.HashAlgorithmName.SHA256);

            for (int i = 0; i < clouds; i++)
            {
                var gammaSeq = KeyGammaSequence.CreateRandom(512);
                encrGammaSequnecesBlend.IntermediateGammaSequneties.Add(gammaSeq);

                _encryptKeyGammaSeq(
                                    pbkdf2,
                                    gammaSeq,
                                    keyGammaSeqEncr,
                                    i,
                                    0);

            }

            for (int i = 0; i < encryptionLayers; i++)
            {

                _encryptKeyGammaSeq(
                                    pbkdf2,
                                    encrGammaSequnecesBlend.TransformKeyGammaSequence(openEncryptionKeyGammaSequnces[i]),
                                    keyGammaSeqEncr,
                                    clouds - i % clouds - 1,
                                    i/clouds + 1);
            }




            FileDataset inputFileDS = new FileDataset();
            inputFileDS.AddFile(filename);
            inputFileDS.FixFileset();

            IConnectedTransform transformation = fileEncrSet.StartTransformation(inputFileDS);

            transformation.Result.RegisterReceiver(receiver, 1024, _noPad);

            using (FileStream stream = File.Open("cloud0\\encrypted.bin", FileMode.Create, FileAccess.Write))
            {
                byte lastLoop = 1;

                while ((transformation.TransformNext()) || (lastLoop-- > 0))
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
        }


        private static void _encryptKeyGammaSeq(
                                        System.Security.Cryptography.Rfc2898DeriveBytes pbkdf2,
                                        KeyGammaSequence keyGammaSequence,
                                        KeyGammaSequenceEncryption keyGammaSeqEncr,
                                        int iteration,
                                        int set
                                        )
        {
            byte[] salt = new byte[64];
            _generator.GetBytes(salt);
            pbkdf2.Salt = salt;
            keyGammaSeqEncr.EncryptionKey = pbkdf2.GetBytes(32);

            if (!Directory.Exists($"cloud{iteration}"))
                Directory.CreateDirectory($"cloud{iteration}");



            using (FileStream keyStream = File.Open($"cloud{iteration}\\key{iteration}_{set}.bin", FileMode.Create, FileAccess.Write))
            {

                byte[][] rawData = keyGammaSeqEncr.Encrypt(new KeyGammaSequence[] { keyGammaSequence });

                keyStream.Write(salt, 0, salt.Length);
                keyStream.Write(rawData[0], 0, rawData[0].Length);

                keyStream.Flush();

                keyStream.Close();
            }
        }
    }
}
