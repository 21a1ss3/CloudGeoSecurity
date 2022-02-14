using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Implements AES-CBC symmetric cipher transformation
    /// </summary>
    public class AesCbcEncryptionTransfrom : ISymmetricCipherTransform
    {
        /// <summary>
        /// Represents implementation IConnectedTransform for AesCbcEncryptionTransfrom
        /// </summary>
        private class _aesCbcConnectedTransform : IConnectedTransform
        {
            /// <summary>
            /// Initialises a new instance of connected transform
            /// </summary>
            /// <param name="sourceSet">The source data set</param>
            /// <param name="key">Encryption key</param>
            /// <param name="isEncrypt">Mode</param>
            /// <param name="padding">Padding</param>
            internal _aesCbcConnectedTransform(IDataSet sourceSet, byte[] key, bool isEncrypt, IDatasetPadding padding)
            {
                Source = sourceSet;
                _resultTransform = new InMemoryDataset(sourceSet);
                _actualResulttranform = _resultTransform;

                if (!isEncrypt)
                {
                    var backwardTransform = new ClearPadTransform();
                    backwardTransform.BlockSize = 128 / 8;
                    backwardTransform.Padding = padding;
                    _backwardTransfConnection = backwardTransform.ConnectWithDataSet(_resultTransform);

                    padding = new NoPadding();

                    _actualResulttranform = _backwardTransfConnection.Result;
                }

                Source.RegisterReceiver(this, 128 / 8, padding);
                _encrypt = isEncrypt;


                _iv.Capacity = Source.Count.Value;
                _aesTransforms = new ICryptoTransform[Source.Count.Value];
                _isFirstTime = new bool[Source.Count.Value];
                for (int i = 0; i < _isFirstTime.Length; i++)
                    _isFirstTime[i] = true;

                _eng.Mode = CipherMode.CBC;
                _eng.Key = key;
            }

            ICryptoTransform[] _aesTransforms;
            private List<byte[]> _iv = new List<byte[]>();

            private bool _encrypt;
            private bool[] _isFirstTime;
            private Aes _eng = Aes.Create();
            private InMemoryDataset _resultTransform;
            private IDataSet _actualResulttranform;
            private IConnectedTransform _backwardTransfConnection;

            /// <summary>
            /// Specifies whether there is more blocks to transform.
            /// </summary>
            public bool MoreAvailable { get; private set; }

            /// <summary>
            /// Gets source dataset
            /// </summary>
            public IDataSet Source { get; private set; }

            /// <summary>
            /// Gets tranformed dataset
            /// </summary>
            public IDataSet Result => _actualResulttranform;

            /// <summary>
            /// Gets encryption key
            /// </summary>
            internal byte[] Key => _eng.Key;

            /// <summary>
            /// Performs transformation of single block of data for each items in source data set
            /// </summary>
            /// <returns>true if there available more blocks to transform, otherwise - false</returns>
            public bool TransformNext()
            {
                if (Source.Count == null)
                    throw new InvalidOperationException("Count shall be determined first!");

                bool more = false;

                for (int i = 0; i < Source.Count; i++)
                {
                    byte[] result = new byte[128 / 8];
                    byte[] source = Source.GetNextBlockForItem(this, i);

                    if (source == null)
                    {
                        if (!_resultTransform.IsFinished(i))
                        {
                            result = _aesTransforms[i].TransformFinalBlock(new byte[0], 0, 0);
                            _resultTransform.WriteToBuffer(i, result);
                        }

                        _resultTransform.FinishItem(i);
                        continue;
                    }

                    more = true;

                    if (source.Length == 0)
                        continue;

                    if (_isFirstTime[i])
                    {
                        _iv.Add(new byte[16]);

                        if (_encrypt)
                        {
                            RandomNumberGenerator rnd = RandomNumberGenerator.Create();
                            rnd.GetNonZeroBytes(result);


                            Array.Copy(result, 0, _iv[i], 0, _iv[i].Length);
                            _resultTransform.WriteToBuffer(i, result);
                        }
                        else
                            Array.Copy(source, 0, _iv[i], 0, _iv[i].Length);

                        _eng.IV = _iv[i];
                        _isFirstTime[i] = false;

                        if (_encrypt)
                            _aesTransforms[i] = _eng.CreateEncryptor();
                        else
                        {
                            _aesTransforms[i] = _eng.CreateDecryptor();

                            continue;
                        }
                    } //if (_isFirst)

                    int transformed = _aesTransforms[i].TransformBlock(source, 0, source.Length, result, 0);
                    if (transformed == 0)
                        continue;

                    Array.Resize(ref result, transformed);

                    _resultTransform.WriteToBuffer(i, result);
                } // for (int i = 0; i < Source.Count; i++)

                if (_backwardTransfConnection!= null)                
                    more = _backwardTransfConnection.TransformNext();
                
                MoreAvailable = more;

                return more;
            }
        }

        private byte[] _key;
        /// <summary>
        /// Gets or sets encryption key. Curretnly supports only 256-bit (32 byte) keys
        /// </summary>
        public byte[] Key
        {
            get { return _key; }
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Key couldn't be null");

                if (value.Length != (256 / 8))
                    throw new InvalidOperationException("Current implementation suppots only 256-bit key!");

                _key = value;
            }
        }


        /// <summary>
        /// Gets or sets encryption mode
        /// </summary>
        public bool Encrypt { get; set; }

        /// <summary>
        /// Gets algorithm name
        /// </summary>
        public string AlgorigthName => "AES256-CBC";

        /// <summary>
        /// Gets or sets data padding using for current transform's inbound data
        /// </summary>
        public IDatasetPadding Padding { get; set; }

        /// <summary>
        /// Gets or sets chiper block size for padding
        /// </summary>
        public ushort BlockSize => 128 / 8;

        /// <summary>
        /// Fixes and connects AesCbcEncryptionTransfrom for specific Dataset and Padding
        /// </summary>
        /// <param name="source">Source dataset</param>
        /// <returns>Connected transfrom, instance of IConnectedTransform</returns>
        public IConnectedTransform ConnectWithDataSet(IDataSet source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (Key == null)
                throw new Exception("Key should be already setted up!");

            if (Padding == null)
                throw new Exception("Padding shall be specified before launching transformation!");

            return new _aesCbcConnectedTransform(source, Key, Encrypt, Padding);
        }
    }
}
