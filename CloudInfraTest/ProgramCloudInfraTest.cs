using System;
using System.IO;
using System.Linq;
using Encryptor.Lib;

namespace CloudInfraTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int encryptionLayersCount = 2;

            FileBasedCloud[] clouds = new FileBasedCloud[3];
            EncryptorEngine engine = new EncryptorEngine();
            object receiver = new object();

            for (int i = 0; i < clouds.Length; i++)
            {
                FileBasedCloud cloud = new FileBasedCloud($"Cloud{i}");
                clouds[i] = cloud;

                engine.CloudStorages.Add(cloud);
            }


            engine.LoadConfiguration();
            EncryptionSet encryptionSet;
            bool reaskPassword = false;
            do
            {
                Console.Write("Proide master password: ");
                string password = Console.ReadLine();

                engine.MasterPassword = password;

                if (engine.EncryptionSets.Count == 0)
                {

                    Guid[] encryptionKeys = engine.CreateEncryptionKeys(encryptionLayersCount);
                    CloudKeyChain blendKeyChain = engine.CreateNewIntermdetiateChain();
                    EncryptionSetItem[] encryptionLayers = new EncryptionSetItem[encryptionLayersCount];

                    for (int i = 0; i < encryptionLayers.Length; i++)
                    {
                        encryptionLayers[i] = new EncryptionSetItem("AesCbcEncryptionTransfrom",
                                                                    (i == 0) ? "RandomPadding" : "NoPadding",
                                                                    32,
                                                                    encryptionKeys[i]);
                    }
                    encryptionSet = engine.CreateEncryptionSet(encryptionLayers, blendKeyChain.KeyChainId);
                }
                else
                {
                    encryptionSet = engine.EncryptionSets.Values.First();
                    reaskPassword = !engine.UnlockKeys(encryptionSet.SetId);
                }
            } while (reaskPassword);

            int operationNumber;

            do
            {
                Console.WriteLine("Choose operation:");
                Console.WriteLine("  1. Encrypt test file");
                Console.WriteLine("  2. Decrypt test file");
                Console.Write("Enter your choice: ");
            } while (!int.TryParse(Console.ReadLine(), out operationNumber) || (operationNumber < 1) || (operationNumber > 2));

            switch (operationNumber)
            {
                case 1:
                    FileDataset sourceFile = new FileDataset();
                    sourceFile.AddFile("TestFile1.txt");
                    sourceFile.FixFileset();

                    ISequenceSet encryptionTransformation = engine.ProduceSequenceSet(encryptionSet, true);
                    IConnectedTransform testFileTransform = encryptionTransformation.StartTransformation(sourceFile);
                    testFileTransform.Result.RegisterReceiver(receiver, 1024, new NoPadding());

                    using (Stream clStream = clouds[0].FileOpenWrite("TestFile1.txt"))
                    {
                        byte lastLoop = 1;

                        clStream.Write(encryptionSet.SetId.ToByteArray());

                        while ((testFileTransform.TransformNext()) || (lastLoop-- > 0))
                        {
                            byte[] rawData;
                            while (((rawData = testFileTransform.Result.GetNextBlockForItem(receiver, 0)) != null) && (rawData.Length > 0))
                            {
                                clStream.Write(rawData, 0, rawData.Length);
                            }

                            clStream.Flush();
                        }

                        clStream.Close();
                    }

                    break;

                case 2:
                    IConnectedTransform transformation = engine.DecryptCloudFile("TestFile1.txt");

                    transformation.Result.RegisterReceiver(receiver, 1024, new NoPadding());
                    using (FileStream stream = File.Open("decrypted.txt", FileMode.Create, FileAccess.Write))
                    {
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
                    }

                    break;
            }


            engine.ProtectNewKeys();
            engine.SaveConfiguration();
        }
    }
}
