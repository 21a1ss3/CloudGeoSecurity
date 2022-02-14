using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Encryptor.Lib
{
    public class EncryptorEngine
    {
        private static readonly string _cloudFolderPrefix = "CloudSecurity";
        private static RandomNumberGenerator _generator = RandomNumberGenerator.Create();

        public List<ICloudStorage> CloudStorages { get; private set; } = new List<ICloudStorage>();
        public Dictionary<Guid, ICloudStorage> KeysMap { get; set; } = new Dictionary<Guid, ICloudStorage>();
        //EncryptionSetId
        public Dictionary<Guid, EncryptionSet> EncryptionSets { get; private set; } = new Dictionary<Guid, EncryptionSet>();
        //KeyChainId
        public Dictionary<Guid, CloudKeyChain> EncryptionKeysChains { get; set; } = new Dictionary<Guid, CloudKeyChain>();

        private Dictionary<Guid, KeyGammaSequence> _encryptionGammaSequencesCache { get; set; } = new Dictionary<Guid, KeyGammaSequence>();
        private Dictionary<Guid, EncryptionEngineTransformCache> _cachedTransforms { get; set; } = new Dictionary<Guid, EncryptionEngineTransformCache>();

        private List<Guid> _recentNewChains = new List<Guid>();
        private List<Guid> _newChains = new List<Guid>();
        private List<EncryptionSet> _newEncryptionSets = new List<EncryptionSet>();

        public void LoadConfiguration()
        {
            string cloudConfigDirPath = $"{_cloudFolderPrefix}\\Configuration";
            string cloudKeysDirPath = $"{_cloudFolderPrefix}\\Configuration\\Keys";
            string cloudTestDirPath = $"{_cloudFolderPrefix}\\Test";
            Encoding utf8 = Encoding.UTF8;

            List<Guid> cloudIds = new List<Guid>(CloudStorages.Count);
            string cummulativeId = string.Empty;

            foreach (var cloud in CloudStorages)
            {
                if (!cloud.DirectoryExists(_cloudFolderPrefix))
                    cloud.CreateDirectory(_cloudFolderPrefix);

                if (!cloud.DirectoryExists(cloudConfigDirPath))
                    cloud.CreateDirectory(cloudConfigDirPath);

                if (!cloud.DirectoryExists(cloudKeysDirPath))
                    cloud.CreateDirectory(cloudKeysDirPath);

                string cloudIdContent = string.Empty;

                if (cloud.FileExists($"{cloudConfigDirPath}\\CloudId"))
                    cloudIdContent = utf8.GetString(cloud.ReadAllBytes($"{cloudConfigDirPath}\\CloudId"));

                if (!string.IsNullOrEmpty(cloudIdContent))
                {
                    Guid id;

                    if (!Guid.TryParse(cloudIdContent, out id))
                        cloudIdContent = string.Empty;
                    else
                        cloud.CloudId = id;
                }

                if (string.IsNullOrEmpty(cloudIdContent))
                {
                    cloud.CloudId = Guid.NewGuid();

                    cloud.WriteAllBytes($"{cloudConfigDirPath}\\CloudId", utf8.GetBytes(cloud.CloudId.ToString()));
                }

                cloudIds.Add(cloud.CloudId);


                ICloudItemInfo[] keysList = cloud.EnumFiles(cloudKeysDirPath);

                foreach (var cloudKey in keysList)
                {
                    if (cloudKey is ICloudFileInfo)
                    {
                        Guid keyId;

                        if (Guid.TryParse(cloudKey.Name, out keyId))
                        {
                            KeysMap.Add(keyId, cloud);
                        }
                    }
                }
            }

            cloudIds.Sort();
            cummulativeId = string.Join(";", cloudIds);


            if (CloudStorages[0].FileExists($"{cloudConfigDirPath}\\EncryptionSet"))
            {
                string encryptionSetsContent = string.Empty;
                string hmacDigit = string.Empty;

                encryptionSetsContent = utf8.GetString(CloudStorages[0].ReadAllBytes($"{cloudConfigDirPath}\\EncryptionSet"));

                using (StringReader reader = new StringReader(encryptionSetsContent))
                {
                    hmacDigit = reader.ReadLine();
                    encryptionSetsContent = reader.ReadToEnd();
                }

                //TODO: check HMAC

                EncryptionSet[] encrSets = Newtonsoft.Json.JsonConvert.DeserializeObject<EncryptionSet[]>(encryptionSetsContent);

                foreach (var encryptionSet in encrSets)
                {
                    EncryptionSets.Add(encryptionSet.SetId, encryptionSet);
                }
            }


            if (CloudStorages[0].FileExists($"{cloudConfigDirPath}\\KeyChains"))
            {
                string keyChainsContent = string.Empty;
                string hmacDigit = string.Empty;

                keyChainsContent = utf8.GetString(CloudStorages[0].ReadAllBytes($"{cloudConfigDirPath}\\KeyChains"));

                using (StringReader reader = new StringReader(keyChainsContent))
                {
                    hmacDigit = reader.ReadLine();
                    keyChainsContent = reader.ReadToEnd();
                }

                //TODO: check HMAC

                CloudKeyChain[] keyChains = Newtonsoft.Json.JsonConvert.DeserializeObject<CloudKeyChain[]>(keyChainsContent);

                foreach (var keyChain in keyChains)
                {
                    EncryptionKeysChains.Add(keyChain.KeyChainId, keyChain);
                }
            }

            if (!CloudStorages[0].DirectoryExists(cloudTestDirPath))
                CloudStorages[0].CreateDirectory(cloudTestDirPath);
        }

        public void SaveConfiguration()
        {
            string cloudConfigDirPath = $"{_cloudFolderPrefix}\\Configuration";
            //string cloudKeysDirPath = $"{_cloudFolderPrefix}\\Configuration\\Keys";


            List<Guid> cloudIds = new List<Guid>(CloudStorages.Count);
            string cummulativeId = string.Empty;
            ProtectNewKeys();

            foreach (var cloud in CloudStorages)
                cloudIds.Add(cloud.CloudId);

            cloudIds.Sort();
            cummulativeId = string.Join(";", cloudIds);

            HMACSHA512 hmac = new HMACSHA512();
            hmac.Key = Encoding.UTF8.GetBytes(cummulativeId);

            
            {
                List<EncryptionSet> encryptionSetsToSave = new List<EncryptionSet>(EncryptionSets.Count);

                foreach (var encryptionSet in EncryptionSets.Values)
                    if (!_newEncryptionSets.Contains(encryptionSet))
                        encryptionSetsToSave.Add(encryptionSet);



                string serEncrSet = Newtonsoft.Json.JsonConvert.SerializeObject(encryptionSetsToSave);
                byte[] rawEncrSet = Encoding.UTF8.GetBytes(serEncrSet);
                byte[] rawHmac = hmac.ComputeHash(rawEncrSet);
                string hmacIntegrity = Convert.ToBase64String(rawHmac);
                byte[] rawHmacStr = Encoding.UTF8.GetBytes(hmacIntegrity + Environment.NewLine);

                using (Stream fileStr = CloudStorages[0].FileOpenWrite($"{cloudConfigDirPath}\\EncryptionSet"))
                {
                    fileStr.Write(rawHmacStr, 0, rawHmacStr.Length);
                    fileStr.Write(rawEncrSet, 0, rawEncrSet.Length);

                    fileStr.Flush();

                    fileStr.Close();
                }
            }


            {
                List<CloudKeyChain> keyChainsToSave = new List<CloudKeyChain>(EncryptionKeysChains.Count);

                foreach (var keyChain in EncryptionKeysChains.Values)
                {
                    if (!_recentNewChains.Contains(keyChain.KeyChainId))
                        keyChainsToSave.Add(keyChain);
                }

                string serKeyChains = Newtonsoft.Json.JsonConvert.SerializeObject(keyChainsToSave);
                byte[] rawKeyChains = Encoding.UTF8.GetBytes(serKeyChains);
                byte[] rawHmac = hmac.ComputeHash(rawKeyChains);
                string hmacIntegrity = Convert.ToBase64String(rawHmac);
                byte[] rawHmacStr = Encoding.UTF8.GetBytes(hmacIntegrity + Environment.NewLine);

                using (Stream fileStr = CloudStorages[0].FileOpenWrite($"{cloudConfigDirPath}\\KeyChains"))
                {
                    fileStr.Write(rawHmacStr, 0, rawHmacStr.Length);
                    fileStr.Write(rawKeyChains, 0, rawKeyChains.Length);

                    fileStr.Flush();

                    fileStr.Close();
                }
            }
        }

        public string MasterPassword { get; set; }
        public bool IsUnlocked { get; private set; }




        public ISequenceSet ProduceSequenceSet(EncryptionSet encryptionSet, bool encrypt)
        {
            EncryptionEngineTransformCache cache = null;

            _cachedTransforms.TryGetValue(encryptionSet.SetId, out cache);

            if (cache == null)
            {
                cache = new EncryptionEngineTransformCache();
                SequenceSet encryptionSequence = new SequenceSet();
                SequenceSet decryptionSequence = new SequenceSet();
                Stack<ISequenceSetTransform> decryptTransformsStack = new Stack<ISequenceSetTransform>(encryptionSet.Encryptions.Count + 1);

                cache.DecryptSequenceSet = decryptionSequence;
                cache.EncryptionSequenceSet = encryptionSequence;

                foreach (var encryptionLayer in encryptionSet.Encryptions)
                {
                    ISymmetricCipherTransform encryptionAlg = null;
                    ISymmetricCipherTransform decryptionAlg = null;
                    IDatasetPadding padding = null;
                    byte[] key = null;

                    if (!_encryptionGammaSequencesCache.ContainsKey(encryptionLayer.KeyId))
                        throw new Exception("Before producing sequence set you must unlock the keys!");

                    key = _encryptionGammaSequencesCache[encryptionLayer.KeyId].ExtractBytes(0, encryptionLayer.KeySize);

                    //TODO: implement proper resolvers

                    switch (encryptionLayer.EncryptionName)
                    {
                        case "AesCbcEncryptionTransfrom":
                            encryptionAlg = new AesCbcEncryptionTransfrom();
                            decryptionAlg = new AesCbcEncryptionTransfrom();
                            break;
                    }

                    switch (encryptionLayer.PaddingName)
                    {
                        case "RandomPadding":
                            padding = new RandomPadding();
                            break;
                        case "NoPadding":
                            padding = new NoPadding();
                            break;
                    }

                    if (encryptionAlg == null)
                        throw new Exception("Unrecognised encryption transformation");

                    if (padding == null)
                        throw new Exception("Unrecognised padding");

                    encryptionAlg.Encrypt = true;
                    decryptionAlg.Encrypt = false;

                    encryptionAlg.Key = key;
                    decryptionAlg.Key = key;

                    Console.Write("Encryption key is: ");
                    foreach (var keyByte in key)
                        Console.Write($"{keyByte:X2}");
                    Console.WriteLine();

                    encryptionAlg.Padding = padding;
                    decryptionAlg.Padding = new NoPadding();

                    decryptTransformsStack.Push(new ClearPadTransform() { Padding = padding, BlockSize = decryptionAlg.BlockSize });

                    encryptionSequence.ScheduleTransformation(encryptionAlg);
                    decryptTransformsStack.Push(decryptionAlg);

                }

                foreach (var decrTr in decryptTransformsStack)
                    decryptionSequence.ScheduleTransformation(decrTr);

                _cachedTransforms.Add(encryptionSet.SetId, cache);
            }

            return encrypt ? cache.EncryptionSequenceSet : cache.DecryptSequenceSet;
        }




        public CloudKeyChain CreateNewIntermdetiateChain()
        {
            CloudKeyChain keyChain = new CloudKeyChain();

            for (int i = 0; i < CloudStorages.Count; i++)
            {
                KeyGammaSequence newImKey = KeyGammaSequence.CreateRandom(510); //2 bytes on a header

                Guid keyId;

                do
                    keyId = Guid.NewGuid();
                while (KeysMap.ContainsKey(keyId) || _encryptionGammaSequencesCache.ContainsKey(keyId));

                _encryptionGammaSequencesCache.Add(keyId, newImKey);

                keyChain.Keys.Add(keyId);
            }

            do
                keyChain.KeyChainId = Guid.NewGuid();
            while (EncryptionKeysChains.ContainsKey(keyChain.KeyChainId));

            EncryptionKeysChains.Add(keyChain.KeyChainId, keyChain);

            if (!_recentNewChains.Contains(keyChain.KeyChainId)) ;
            _recentNewChains.Add(keyChain.KeyChainId);

            return keyChain;
        }

        public Guid[] CreateEncryptionKeys(int count)
        {
            Guid[] keys = new Guid[count];

            for (int i = 0; i < count; i++)
            {
                KeyGammaSequence newImKey = KeyGammaSequence.CreateRandom(510);

                Guid keyId;

                do
                    keyId = Guid.NewGuid();
                while (KeysMap.ContainsKey(keyId) || _encryptionGammaSequencesCache.ContainsKey(keyId));

                _encryptionGammaSequencesCache.Add(keyId, newImKey);

                keys[i] = keyId;
            }

            return keys;
        }

        public EncryptionSet CreateEncryptionSet(EncryptionSetItem[] encryptionLevels, Guid keyChainId)
        {
            Guid encryptionSetId;

            do
                encryptionSetId = Guid.NewGuid();
            while (EncryptionSets.ContainsKey(encryptionSetId));

            EncryptionSet encryptionSet = new EncryptionSet(encryptionSetId, keyChainId, encryptionLevels);
            EncryptionSets.Add(encryptionSetId, encryptionSet);

            if (_recentNewChains.Contains(keyChainId) && !_newChains.Contains(keyChainId))
                _newChains.Add(keyChainId);

            _newEncryptionSets.Add(encryptionSet);

            return encryptionSet;
        }


        public bool UnlockKeys(Guid setId)
        {
            string cloudTestFilePath = $"{_cloudFolderPrefix}\\Test\\{setId}";

            var pbkdf2 = new Rfc2898DeriveBytes(
               Encoding.UTF8.GetBytes(MasterPassword),
               new byte[64], 2048, HashAlgorithmName.SHA256);

            List<Guid> keysToReset = new List<Guid>();
            EncryptionSet encryptionSet = EncryptionSets[setId];
            CloudKeyChain chain = EncryptionKeysChains[encryptionSet.KeyChain];
            KeyGammaSequenceBlend blendHelper = new KeyGammaSequenceBlend();

            foreach (var keyId in chain.Keys)
            {
                if (!_encryptionGammaSequencesCache.ContainsKey(keyId))
                {
                    _encryptionGammaSequencesCache.Add(keyId, _readKeyGammaSequenceFromCloud(keyId, pbkdf2));

                    if (!keysToReset.Contains(keyId))
                        keysToReset.Add(keyId);
                }

                blendHelper.IntermediateGammaSequneties.Add(_encryptionGammaSequencesCache[keyId]);
            }

            foreach (var encryptionInfo in encryptionSet.Encryptions)
            {

                if (!_encryptionGammaSequencesCache.ContainsKey(encryptionInfo.KeyId))
                {
                    _encryptionGammaSequencesCache.Add(encryptionInfo.KeyId, _readKeyGammaSequenceFromCloud(encryptionInfo.KeyId, pbkdf2));

                    if (!keysToReset.Contains(encryptionInfo.KeyId))
                        keysToReset.Add(encryptionInfo.KeyId);

                    _encryptionGammaSequencesCache[encryptionInfo.KeyId] = blendHelper.TransformKeyGammaSequence(_encryptionGammaSequencesCache[encryptionInfo.KeyId]);
                }
            }

            // If there was no keys are actully loaded from cloud
            // Then we have unlocked all of them
            // And there no need to perfrom anything
            if (keysToReset.Count == 0)
                return true;

            ISequenceSet decryptSeq = ProduceSequenceSet(encryptionSet, false);

            CloudFileDataset cloudFileDataset = new CloudFileDataset(CloudStorages[0]);
            cloudFileDataset.AddFile(cloudTestFilePath);
            cloudFileDataset.FixFileset();

            IConnectedTransform testDecryptTransfrom = decryptSeq.StartTransformation(cloudFileDataset);
            testDecryptTransfrom.Result.RegisterReceiver(this, 1024, new NoPadding());

            MemoryStream bufferStream = new MemoryStream();


            while (testDecryptTransfrom.TransformNext()) ;

            {
                byte[] rawData;
                while ((rawData = testDecryptTransfrom.Result.GetNextBlockForItem(this, 0)) != null)
                {
                    bufferStream.Write(rawData, 0, rawData.Length);
                }
            }

            bufferStream.Position = 0;

            SHA512 integrityChecker = SHA512.Create();

            byte[] hashToCompare = new byte[integrityChecker.HashSize / 8];
            byte[] headerToCheck = new byte[bufferStream.Length - hashToCompare.Length];

            bufferStream.Read(headerToCheck, 0, headerToCheck.Length);
            bufferStream.Read(hashToCompare, 0, hashToCompare.Length);

            bufferStream.Close();

            byte[] calculatedHash = integrityChecker.ComputeHash(headerToCheck);

            bool valid = true;

            for (int i = 0; i < hashToCompare.Length; i++)
                if (hashToCompare[i] != calculatedHash[i])
                    valid = false;

            if (!valid)
            {
                _cachedTransforms.Remove(encryptionSet.SetId);

                foreach (var keyId in keysToReset)
                    _encryptionGammaSequencesCache.Remove(keyId);
            }
            else
                IsUnlocked = true;

            return valid;
        }

        public void ProtectNewKeys()
        {
            string cloudKeysDirPath = $"{_cloudFolderPrefix}\\Configuration\\Keys";
            var pbkdf2 = new Rfc2898DeriveBytes(
               Encoding.UTF8.GetBytes(MasterPassword),
               new byte[64], 2048, HashAlgorithmName.SHA256);

            KeyGammaSequenceEncryption keyGammaSeqEncr = new KeyGammaSequenceEncryption();
            keyGammaSeqEncr.Cipher = new AesCbcEncryptionTransfrom();
            //keyGammaSeqEncr.Cipher.Padding = new RandomPadding();
            keyGammaSeqEncr.Cipher.Padding = new NoPadding();

            Dictionary<Guid, KeyGammaSequenceBlend> blendTransforms = new Dictionary<Guid, KeyGammaSequenceBlend>();


            foreach (var chainId in _newChains)
            {
                CloudKeyChain chain = EncryptionKeysChains[chainId];
                KeyGammaSequenceBlend keyBlend = new KeyGammaSequenceBlend();

                for (int i = 0; i < chain.Keys.Count; i++)
                {
                    KeyGammaSequence intermediateKey = _encryptionGammaSequencesCache[chain.Keys[i]];

                    byte[] protectedKey = _protectKeyGammaSequence(intermediateKey, pbkdf2, keyGammaSeqEncr);

                    CloudStorages[i].WriteAllBytes($"{cloudKeysDirPath}\\{chain.Keys[i]}", protectedKey);

                    keyBlend.IntermediateGammaSequneties.Add(intermediateKey);
                }

                blendTransforms.Add(chainId, keyBlend);
                _recentNewChains.Remove(chainId);
            }

            _newChains.Clear();


            foreach (var encrSet in _newEncryptionSets)
            {
                string cloudTestFilePath = $"{_cloudFolderPrefix}\\Test\\{encrSet.SetId}";

                KeyGammaSequenceBlend keyBlend = null;

                blendTransforms.TryGetValue(encrSet.KeyChain, out keyBlend);

                if (keyBlend == null)
                {
                    CloudKeyChain chain = EncryptionKeysChains[encrSet.KeyChain];
                    keyBlend = new KeyGammaSequenceBlend();

                    for (int i = 0; i < chain.Keys.Count; i++)
                        keyBlend.IntermediateGammaSequneties.Add(_encryptionGammaSequencesCache[chain.Keys[i]]);
                }

                for (int i = 0; i < encrSet.Encryptions.Count; i++)
                {
                    KeyGammaSequence keyGamma = _encryptionGammaSequencesCache[encrSet.Encryptions[i].KeyId];

                    keyGamma = keyBlend.TransformKeyGammaSequence(keyGamma);

                    byte[] protectedKey = _protectKeyGammaSequence(keyGamma, pbkdf2, keyGammaSeqEncr);
                    ICloudStorage cloud = CloudStorages[CloudStorages.Count - i % CloudStorages.Count - 1];
                    cloud.WriteAllBytes($"{cloudKeysDirPath}\\{encrSet.Encryptions[i].KeyId}", protectedKey);
                }


                InMemoryDataset integrityInfo = new InMemoryDataset(1);
                ISequenceSet encryptSeq = ProduceSequenceSet(encrSet, true);
                IConnectedTransform testEncryptTransfrom = encryptSeq.StartTransformation(integrityInfo);
                testEncryptTransfrom.Result.RegisterReceiver(this, 1024, new NoPadding());

                {
                    SHA512 integrityChecker = SHA512.Create();

                    byte[] integrityPasswordPadding = new byte[512];
                    _generator.GetBytes(integrityPasswordPadding);
                    byte[] integrityCheck = integrityChecker.ComputeHash(integrityPasswordPadding);

                    integrityInfo.WriteToBuffer(0, integrityPasswordPadding);
                    integrityInfo.WriteToBuffer(0, integrityCheck);
                }

                integrityInfo.FinishItem(0);

                while (testEncryptTransfrom.TransformNext()) ;


                using (Stream writeStream = CloudStorages[0].FileOpenWrite(cloudTestFilePath))
                {
                    byte[] rawData;
                    while ((rawData = testEncryptTransfrom.Result.GetNextBlockForItem(this, 0)) != null)
                    {
                        writeStream.Write(rawData, 0, rawData.Length);
                    }

                    writeStream.Flush();
                    writeStream.Close();
                }
            }

            _newEncryptionSets.Clear();
        }



        private byte[] _protectKeyGammaSequence(KeyGammaSequence keyGammaSequence, Rfc2898DeriveBytes pbkdf2, KeyGammaSequenceEncryption keyGammaSeqEncr)
        {

            byte[] salt = new byte[64];
            _generator.GetBytes(salt);
            pbkdf2.Salt = salt;
            keyGammaSeqEncr.EncryptionKey = pbkdf2.GetBytes(32);

            MemoryStream bufferStream = new MemoryStream();

            byte[][] rawData = keyGammaSeqEncr.Encrypt(new KeyGammaSequence[] { keyGammaSequence });
            bufferStream.Write(salt, 0, salt.Length);
            bufferStream.Write(rawData[0], 0, rawData[0].Length);

            bufferStream.Flush();

            byte[] result = bufferStream.ToArray();

            bufferStream.Close();

            return result;
        }

        private KeyGammaSequence _readKeyGammaSequenceFromCloud(Guid keyId, Rfc2898DeriveBytes pbkdf2)
        {
            string cloudKeysDirPath = $"{_cloudFolderPrefix}\\Configuration\\Keys";

            KeyGammaSequenceEncryption keyGammaSeqEncr = new KeyGammaSequenceEncryption();
            keyGammaSeqEncr.Cipher = new AesCbcEncryptionTransfrom();
            //keyGammaSeqEncr.Cipher.Padding = new RandomPadding();
            keyGammaSeqEncr.Cipher.Padding = new NoPadding();

            byte[] salt = new byte[64];

            long size = KeysMap[keyId].GetFileSize($"{cloudKeysDirPath}\\{keyId}");
            byte[] rawGammaSequence = new byte[size - salt.Length];

            using (Stream rawKeyStream = KeysMap[keyId].FileOpenRead($"{cloudKeysDirPath}\\{keyId}"))
            {
                int readBytes = 0;

                do
                    readBytes += rawKeyStream.Read(salt, readBytes, salt.Length - readBytes);
                while (readBytes < salt.Length);

                readBytes = 0;

                do
                    readBytes += rawKeyStream.Read(rawGammaSequence, readBytes, rawGammaSequence.Length - readBytes);
                while (readBytes < rawGammaSequence.Length);

                rawKeyStream.Close();
            }

            pbkdf2.Salt = salt;

            keyGammaSeqEncr.EncryptionKey = pbkdf2.GetBytes(32);

            KeyGammaSequence[] decrypted = keyGammaSeqEncr.Decrypt(new byte[][] { rawGammaSequence });

            return decrypted[0];
        }

        public IConnectedTransform DecryptCloudFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (!CloudStorages[0].FileExists(path))
                throw new Exception($"File {path} not found");

            byte[] encryptionSet = CloudStorages[0].ReadFirstBytes(path, 16);

            Guid encrSetId = new Guid(encryptionSet);
            EncryptionSet encrSet = EncryptionSets[encrSetId];

            ISequenceSet sequenceSet = ProduceSequenceSet(encrSet, false);
            CloudFileDataset dataSet = new CloudFileDataset(CloudStorages[0]);

            dataSet.AddFile(path, 16);
            dataSet.FixFileset();

            return sequenceSet.StartTransformation(dataSet);
        }


    }
}
