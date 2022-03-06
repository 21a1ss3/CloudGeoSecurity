using System;
using System.IO;
using System.Linq;
using Encryptor.Lib;
using OneDrive = Encryptor.Lib.OneDrive;
using GDrive = Encryptor.Lib.GDrive;
using YDrive = Encryptor.Lib.Yandex;
using Cache = Encryptor.Lib.Cache;

namespace CloudInfraTest
{
    class Program
    {
        static void Main(string[] args)
        {
            com_Main(args);
            //TestOneDrive(args);
            //TestGDrive(args);
            //TestYDrive(args);
        }

        static void TestYDrive(string[] args)
        {
            YDrive.YandexDriveCloud yCloud = new YDrive.YandexDriveCloud();
            Cache.ChunkCache cache = new Cache.ChunkCache(1 << 16);
            yCloud.Cache = cache;

            yCloud.AuthentificationAction += YCloud_AuthentificationAction;

            bool status = yCloud.SignIn(YDrive.YandexAuthOptions.DeviceCode);

            Console.WriteLine($"status: {status}; isAuth: {yCloud.IsAuthenticated}");

            if (yCloud.IsAuthenticated)
            {
                var items = yCloud.EnumItems("TestFolder");

                foreach (var item in items)
                {
                    Console.WriteLine("***************");
                    Console.WriteLine($"  Name: {item.Name}, Container: {item.ContainerPath}, FullPath: {item.FullPath}, isFile: {item is ICloudFileInfo}");
                    Console.WriteLine("################");
                }
            }
        }

        private static void YCloud_AuthentificationAction(object sender, YDrive.YandexActionInfo e)
        {
            switch (e.ActionType)
            {
                case YDrive.YandexActionType.OpenBrowser:
                    break;
                case YDrive.YandexActionType.DeviceCodeDisplay:
                    Console.WriteLine($"Please visit {e.DeivceCodeUrl} and enter this code: {e.DeviceCode}");

                    break;
            }
        }

        static void TestGDrive(string[] args)
        {
            GDrive.GoogleDriveCloud cloud = new GDrive.GoogleDriveCloud();
            Cache.ChunkCache cache = new Cache.ChunkCache(1 << 16);
            cloud.Cache = cache;

            cloud.AuthentificationAction += Cloud_AuthentificationAction1;

            bool status = cloud.SignIn(GDrive.GAuthOptions.CustomBrowser);

            Console.WriteLine($"status: {status}; isAuth: {cloud.IsAuthenticated}");

            if (cloud.IsAuthenticated)
            {
                if (!cloud.DirectoryExists("DemoDir"))
                {
                    cloud.CreateDirectory("DemoDir");
                    Console.WriteLine("Directory created!");
                }

                var items = cloud.EnumItems("");

                foreach (var item in items)
                {
                    Console.WriteLine("***************");
                    Console.WriteLine($"  Name: {item.Name}, Container: {item.ContainerPath}, FullPath: {item.FullPath}, isFile: {item is ICloudFileInfo}");
                    Console.WriteLine("################");
                }
            }
        }

        private static void Cloud_AuthentificationAction1(object sender, GDrive.GAuthActionInfo e)
        {
            switch (e.ActionType)
            {
                case GDrive.GAuthActionType.LaunchBrowser:
                    Console.WriteLine("Please visit this URL:");
                    Console.WriteLine();
                    Console.WriteLine(e.WebBrowserUri);
                    Console.WriteLine();

                    break;
                default:
                    break;
            }
        }

        static void TestOneDrive(string[] args)
        {
            OneDrive.OneDriveCloud cloud = new OneDrive.OneDriveCloud();
            Cache.ChunkCache cache = new Cache.ChunkCache(1<<16);
            cloud.Cache = cache;

            cloud.AuthentificationAction += Cloud_AuthentificationAction;

            bool status = cloud.SignIn(OneDrive.MsalAuthOption.EmbededBrowser);

            Console.WriteLine($"status: {status}; isAuth: {cloud.IsAuthenticated}");

            if(cloud.IsAuthenticated)
            {
                var items = cloud.EnumItems("");

                foreach (var item in items)
                {
                    Console.WriteLine("***************");
                    Console.WriteLine($"  Name: {item.Name}, Container: {item.ContainerPath}, FullPath: {item.FullPath}, isFile: {item is ICloudFileInfo}");
                    Console.WriteLine("################");
                }
            }
        }

        private static void Cloud_AuthentificationAction(object sender, OneDrive.MsalAuthActionInfo e)
        {
            switch (e.MsalAuthAction)
            {
                case OneDrive.MsalAuthActionKind.DeviceCodeDisplay:
                    Console.WriteLine($"Please visit {e.DeviceCodeVisitUrl} and enter this code: {e.DeviceCode}");
                    break;
                case OneDrive.MsalAuthActionKind.OpenBrowser:
                    Console.WriteLine("Please visit this URL:");
                    Console.WriteLine();
                    Console.WriteLine(e.WebBrowserUrl);
                    Console.WriteLine();
                    break;
            }
            
        }

        static void com_Main(string[] args)
        {
            int encryptionLayersCount = 2;

            ICloudStorage[] clouds = new ICloudStorage[3];
            EncryptorEngine engine = new EncryptorEngine();
            Cache.ChunkCache cache = new Cache.ChunkCache(1 << 16);
            object receiver = new object();

            {
                OneDrive.OneDriveCloud oneDriveCloud = new OneDrive.OneDriveCloud();
                oneDriveCloud.Cache = cache;

                oneDriveCloud.AuthentificationAction += Cloud_AuthentificationAction;

                bool status = oneDriveCloud.SignIn(OneDrive.MsalAuthOption.EmbededBrowser);

                if (!status)
                {
                    Console.WriteLine("Unable to proceed! User msut successfully log into OneDirve!");
                    return;
                }


                clouds[0] = oneDriveCloud;
                engine.CloudStorages.Add(oneDriveCloud);
            }

            {
                GDrive.GoogleDriveCloud gCloud = new GDrive.GoogleDriveCloud();
                gCloud.Cache = cache;
                gCloud.AuthentificationAction += Cloud_AuthentificationAction1;

                bool status = gCloud.SignIn(GDrive.GAuthOptions.CustomBrowser);
                
                if (!status)
                {
                    Console.WriteLine("Unable to proceed! User msut successfully log into Google drive!");
                    return;
                }

                clouds[1] = gCloud;
                engine.CloudStorages.Add(gCloud);

            }
            {
                YDrive.YandexDriveCloud yCloud = new YDrive.YandexDriveCloud();
                yCloud.Cache = cache;

                yCloud.AuthentificationAction += YCloud_AuthentificationAction;

                bool status = yCloud.SignIn(YDrive.YandexAuthOptions.DeviceCode);

                Console.WriteLine($"status: {status}; isAuth: {yCloud.IsAuthenticated}");
                if (!status)
                {
                    Console.WriteLine("Unable to proceed! User msut successfully log into Ynadex Disk!");
                    return;
                }

                clouds[2] = yCloud;
                engine.CloudStorages.Add(yCloud);
            }

            //for (int i = 2; i < clouds.Length; i++)
            //{
            //    FileBasedCloud cloud = new FileBasedCloud($"Cloud{i}");
            //    clouds[i] = cloud;

            //    engine.CloudStorages.Add(cloud);
            //}
                        

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
                        }


                        clStream.Flush();
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
