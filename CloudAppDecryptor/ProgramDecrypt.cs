using Encryptor.Lib;
using System;
using System.IO;

namespace CloudAppDecryptor
{
    class ProgramDecrypt
    {
        static void Main(string[] args)
        {
            SequenceSet encrSet = new SequenceSet();

            RandomPadding rndPadding = new RandomPadding();
            NoPadding noPad = new NoPadding();

            KeyEntropy[] keys = new KeyEntropy[3];
            KeyEntropyEncryption keyEntropyEncryption = new KeyEntropyEncryption();
            KeyEntropyTransform keyEntropyTransform = new KeyEntropyTransform();
            byte[][] encryptedIntermediateEntropy = new byte[keys.Length][];
            byte[][] encryptedKeys = new byte[keys.Length][];
            byte[] salt = File.ReadAllBytes($"salt.bin");


            Console.Write("Proide master password: ");
            string password = Console.ReadLine();
            
            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                System.Text.Encoding.UTF8.GetBytes(password),
                salt, 2048, System.Security.Cryptography.HashAlgorithmName.SHA256);

            keyEntropyEncryption.EncryptionKey = pbkdf2.GetBytes(32);
            
            {
                AesCbcEncryptionTransfrom keyTransform = new AesCbcEncryptionTransfrom();
                keyTransform.Padding = rndPadding;
                keyEntropyEncryption.Cipher = keyTransform;
            }


            for (int i = keys.Length - 1; i > -1; i--)
            {
                encryptedIntermediateEntropy[i] = File.ReadAllBytes($"key{i}_0.bin");
                encryptedKeys[i] = File.ReadAllBytes($"key{i}_1.bin");
            }

            keyEntropyTransform.IntermediateEntropies.AddRange(
                    keyEntropyEncryption.Decrypt(encryptedIntermediateEntropy)
                );

            keys = keyEntropyEncryption.Decrypt(encryptedKeys);

            for (int i = keys.Length - 1; i > -1; i--)
            {
                keys[i] = keyEntropyTransform.TransformKeyEntropy(keys[i]);

                AesCbcEncryptionTransfrom transform = new AesCbcEncryptionTransfrom();
                
                transform.Key = keys[i].ExtractBytes(0, 32);
                transform.Encrypt = false;
                transform.Padding = rndPadding;

                encrSet.ScheduleTransformation(transform);

                /*ClearPadTransform clearPad = new ClearPadTransform();
                clearPad.Padding = rndPadding;
                clearPad.BlockSize = 16;
                encrSet.ScheduleTransformation(clearPad);*/
            }


            FileDataset inputFileDS = new FileDataset();
            inputFileDS.AddFile("encrypted.bin");
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
        //static void oldMain2(string[] args)
        //{
        //    SequenceSet encrSet = new SequenceSet();

        //    RandomPadding rndPadding = new RandomPadding();
        //    NoPadding noPad = new NoPadding();

        //    byte[][] keys = new byte[3][];
        //    for (int i = keys.Length - 1; i > -1; i--)
        //    {
        //        AesCbcEncryptionTransfrom transform = new AesCbcEncryptionTransfrom();

        //        keys[i] = File.ReadAllBytes($"key{i}.bin");

        //        transform.Key = keys[i];
        //        transform.Encrypt = false;
        //        transform.Padding = rndPadding;

        //        encrSet.ScheduleTransformation(transform);

        //        /*ClearPadTransform clearPad = new ClearPadTransform();
        //        clearPad.Padding = rndPadding;
        //        clearPad.BlockSize = 16;
        //        encrSet.ScheduleTransformation(clearPad);*/
        //    }


        //    FileDataset inputFileDS = new FileDataset();
        //    inputFileDS.AddFile("encrypted.bin");
        //    inputFileDS.FixFileset();


        //    IConnectedTransform transformation = encrSet.StartTransformation(inputFileDS);
        //    object receiver = new object();
        //    transformation.Result.RegisterReceiver(receiver, 1024, noPad);

        //    FileStream stream = File.Open("decrypted.txt", FileMode.Create, FileAccess.Write);

        //    byte lastLoop = 1;

        //    while (transformation.TransformNext() || (lastLoop-- > 0))
        //    {
        //        byte[] rawData;
        //        while (((rawData = transformation.Result.GetNextBlockForItem(receiver, 0)) != null) && (rawData.Length > 0))
        //        {
        //            stream.Write(rawData, 0, rawData.Length);
        //        }

        //        stream.Flush();
        //    }

        //    stream.Close();
        //}

        //static void oldMain(string[] args)
        //{
        //    EncryptorEngine enc = new EncryptorEngine();

        //    Console.WriteLine("Proide filename to decrypt: ");
        //    string filename = Console.ReadLine();

        //    byte[] srcFile = File.ReadAllBytes($"{filename}.enc");

        //    byte[][] secSeq = new byte[3][];

        //    for (int i = 0; i < secSeq.Length; i++)
        //        secSeq[i] = new byte[32];

        //    for (int i = 0; i < secSeq.Length; i++)
        //    {
        //        BinaryReader keysReader = new BinaryReader(File.OpenRead($"keyst{i}.bin"));

        //        byte key = keysReader.ReadByte();
        //        byte seg = keysReader.ReadByte();
        //        int len = keysReader.ReadInt32();

        //        byte[] bytes = keysReader.ReadBytes(len);

        //        Array.Copy(bytes, 0, secSeq[key], seg * secSeq[key].Length / 2, len);
        //    }

        //    Console.WriteLine("Provide master password:");
        //    byte[] password = System.Text.Encoding.UTF8.GetBytes(Console.ReadLine());

        //    for (int i = 0; i < secSeq.Length; i++)
        //        secSeq[i] = enc.TransformKey(secSeq[i], password, false);

        //    byte[] plainFile;
        //    try
        //    {
        //        plainFile = enc.DecryptContent(srcFile, secSeq);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Unable to decrypt file. Double check the password. Exception: {ex}");
        //        return;
        //    }

        //    File.WriteAllBytes($"{filename}.plain", plainFile);
        //}
    }
}
