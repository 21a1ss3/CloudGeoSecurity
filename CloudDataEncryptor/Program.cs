using Encryptor.Lib;
using System;
using System.IO;
using System.Linq;

namespace CloudDataEncryptor
{
    class Program
    {
        static void Main(string[] args)
        {
            SequenceSet encrSet = new SequenceSet();

            //byte[][] keys = new byte[3][];
            var generator = System.Security.Cryptography.RandomNumberGenerator.Create();
            RandomPadding rndPadding = new RandomPadding();
            NoPadding noPad = new NoPadding();
            KeyEntropyTransform keyEntropyTransform = new KeyEntropyTransform();
            KeyEntropy[] keys = new KeyEntropy[3];
            KeyEntropyEncryption keyEntropyEncryption = new KeyEntropyEncryption();

            {
                AesCbcEncryptionTransfrom keyTransform = new AesCbcEncryptionTransfrom();
                keyTransform.Padding = rndPadding;
                keyEntropyEncryption.Cipher = keyTransform;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                keyEntropyTransform.IntermediateEntropies.Add(KeyEntropy.CreateRandom(512));
                keys[i] = KeyEntropy.CreateRandom(512);

                AesCbcEncryptionTransfrom transform = new AesCbcEncryptionTransfrom();
                //keys[i] = new byte[32];

                //generator.GetNonZeroBytes(keys[i]);

                transform.Key = keys[i].ExtractBytes(0, 32);
                transform.Encrypt = true;
                transform.Padding = rndPadding;

                encrSet.ScheduleTransformation(transform);


                //FileStream keyStream = File.Open($"key{i}.bin", FileMode.Create, FileAccess.Write);
                //keyStream.Write(keys[i], 0, keys[i].Length);
                //keyStream.Flush();
                //keyStream.Close();
            }

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keyEntropyTransform.TransformKeyEntropy(keys[i]);
            }

            Console.Write("Proide filename to encrypt: ");
            string filename = Console.ReadLine();

            Console.Write("Proide master password: ");
            string password = Console.ReadLine();

            byte[] salt = new byte[16];
            generator.GetBytes(salt);

            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                System.Text.Encoding.UTF8.GetBytes(password),
                salt, 2048, System.Security.Cryptography.HashAlgorithmName.SHA256);

            keyEntropyEncryption.EncryptionKey = pbkdf2.GetBytes(32);

            byte[][] encryptedIntermediateEntropy = keyEntropyEncryption.Encrypt(keyEntropyTransform.IntermediateEntropies);
            byte[][] encryptedKeys = keyEntropyEncryption.Encrypt(keys);


            for (int i = 0; i < keys.Length; i++)
            {
                FileStream keyStream = File.Open($"key{i}_0.bin", FileMode.Create, FileAccess.Write);
                keyStream.Write(encryptedIntermediateEntropy[i], 0, encryptedIntermediateEntropy[i].Length);
                keyStream.Flush();
                keyStream.Close();

                keyStream = File.Open($"key{i}_1.bin", FileMode.Create, FileAccess.Write);
                keyStream.Write(encryptedKeys[i], 0, encryptedKeys[i].Length);
                keyStream.Flush();
                keyStream.Close();
            }

            {
                FileStream saltStream = File.Open($"salt.bin", FileMode.Create, FileAccess.Write);
                saltStream.Write(salt, 0, salt.Length);
                saltStream.Flush();
                saltStream.Close();
            }


            FileDataset inputFileDS = new FileDataset();
            inputFileDS.AddFile(filename);
            inputFileDS.FixFileset();

            IConnectedTransform transformation = encrSet.StartTransformation(inputFileDS);
            object receiver = new object();
            transformation.Result.RegisterReceiver(receiver, 1024, noPad);

            FileStream stream = File.Open("encrypted.bin", FileMode.Create, FileAccess.Write);

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


        //static void oldMain(string[] args)
        //{
        //    EncryptorEngine enc = new EncryptorEngine();

        //    Console.WriteLine("Proide filename to encrypt: ");
        //    string filename = Console.ReadLine();

        //    byte[] srcFile = File.ReadAllBytes(filename);

        //    byte[][] secSeq = new byte[3][];

        //    for (int i = 0; i < secSeq.Length; i++)
        //        secSeq[i] = enc.GenerateKey(32);

        //    byte[] encSrc = enc.EncryptContent(srcFile, secSeq);

        //    File.WriteAllBytes($"{filename}.enc", encSrc);

        //    Console.WriteLine("Provide master password:");
        //    byte[] password = System.Text.Encoding.UTF8.GetBytes(Console.ReadLine());

        //    for (int i = 0; i < secSeq.Length; i++)
        //        secSeq[i] = enc.TransformKey(secSeq[i], password, true);

        //    BinaryWriter keySegmentsWriter;

        //    keySegmentsWriter = new BinaryWriter(File.Open("keyst0.bin", FileMode.Create, FileAccess.Write));
        //    _writeSegment(keySegmentsWriter, 1, 0, secSeq);
        //    _writeSegment(keySegmentsWriter, 2, 1, secSeq);
        //    keySegmentsWriter.Flush();
        //    keySegmentsWriter.Close();

        //    keySegmentsWriter = new BinaryWriter(File.Open("keyst1.bin", FileMode.Create, FileAccess.Write));
        //    _writeSegment(keySegmentsWriter, 0, 0, secSeq);
        //    _writeSegment(keySegmentsWriter, 2, 2, secSeq);
        //    keySegmentsWriter.Flush();
        //    keySegmentsWriter.Close();

        //    keySegmentsWriter = new BinaryWriter(File.Open("keyst2.bin", FileMode.Create, FileAccess.Write));
        //    _writeSegment(keySegmentsWriter, 1, 1, secSeq);
        //    _writeSegment(keySegmentsWriter, 0, 1, secSeq);
        //    keySegmentsWriter.Flush();
        //    keySegmentsWriter.Close();


        //    Console.WriteLine("Done");
        //}

        private static void _writeSegment(BinaryWriter keySegmentsWriter, byte keyNum, byte segNum, byte[][] secSeq)
        {
            keySegmentsWriter.Write(keyNum);
            keySegmentsWriter.Write(segNum);
            keySegmentsWriter.Write((int)(secSeq[keyNum].Length / 2));
            keySegmentsWriter.Write(secSeq[keyNum].Skip(segNum * secSeq[keyNum].Length / 2).Take(secSeq[keyNum].Length / 2).ToArray());
        }

    }
}
