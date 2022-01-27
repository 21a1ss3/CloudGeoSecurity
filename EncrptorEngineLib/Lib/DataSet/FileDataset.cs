using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents a file-based dataset
    /// </summary>
    public class FileDataset : DataSetBase
    {
        /// <summary>
        /// Initialises a new file-based dataset instance
        /// </summary>
        public FileDataset() : base(count: null)
        {
            RequireRecieverPropertyType<FileReceiverProperties>();
        }


        /// <summary>
        /// Represents internal datastrucutre which describes a single file in the data set
        /// </summary>
        private class FileInfo
        {
            /// <summary>
            /// Gets or sets file name
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// Gets or sets file stream
            /// </summary>
            public FileStream Stream { get; set; }
        }

        /// <summary>
        /// Provides hidden extension for ReceiverProperties
        /// </summary>
        private interface IReaderState
        {
            public int CurrentPosition { get; set; }
            public bool ReachedEof { get; set; }
        }

        /// <summary>
        /// Receiver state for a file of specific receiver
        /// </summary>
        protected class ReaderState : IReaderState
        {
            /// <summary>
            /// Gets current read position for specific file of specific receiver
            /// </summary>
            public int CurrentPosition { get; private set; }

            /// <summary>
            /// Gets a value that indicates whether current receiver reached the end of stream
            /// </summary>
            public bool ReachedEof { get; private set; } = false;

            int IReaderState.CurrentPosition { get => CurrentPosition; set => CurrentPosition = value; }
            bool IReaderState.ReachedEof { get => ReachedEof; set => ReachedEof = value; }
        }

        /// <summary>
        /// Holds file receiver properties
        /// </summary>
        protected class FileReceiverProperties : ReceiverProperties
        {
            /// <summary>
            /// States of receiving each file from dataset for specific receiver
            /// </summary>
            public ReaderState[] ReadStates { get; set; }
        }      

        /// <summary>
        /// Holds list of source files in dataset and their filestream
        /// </summary>
        private List<FileInfo> _sources = new List<FileInfo>();

        /// <summary>
        /// Adds file in the dataset. Must be called before data set is marking as fixed.
        /// </summary>
        /// <param name="filename">The file name to add</param>
        public void AddFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));

            //TODO: check on file existance

            if (Count != null)
                throw new InvalidOperationException("File dataset is fixed!");
            

            if (_sources.Where(fInfo => fInfo.FileName == filename).Count() == 0)
                _sources.Add(new FileInfo() { FileName = filename });
        }

        /// <summary>
        /// Fixes from dataset further modification and determines Count property.
        /// </summary>
        public void FixFileset()
        {
            if (Count != null)
                throw new InvalidOperationException("File dataset must be unfixed");

            Count = _sources.Count;
        }

        /// <summary>
        /// Overrides DataSetBase.OnBeforeRegisterReceiver and configures receiver properties
        /// </summary>
        /// <param name="receiver">Registring receiver</param>
        /// <param name="properties">Instance of properties</param>
        protected override void OnBeforeRegisterReceiver(object receiver, ReceiverProperties properties)
        {
            base.OnBeforeRegisterReceiver(receiver, properties);

            if (Count != null)
            {
                FileReceiverProperties fileProperties = (FileReceiverProperties)properties;

                fileProperties.ReadStates = new ReaderState[Count.Value];
            }            
        }

        /// <summary>
        /// Overrides DataSetBase.OnCountDetermined
        /// </summary>
        /// <param name="newCount"></param>
        protected override void OnCountDetermined(int newCount)
        {
            base.OnCountDetermined(newCount);

            foreach (FileReceiverProperties fileProp in Receivers.Values)
            {
                fileProp.ReadStates = new ReaderState[newCount];
            }
        }

        /// <summary>
        /// Overrides DataSetBase.GetNextBlockForItemCore and returning requested source data from the file.
        /// </summary>
        /// <param name="receiver">An object representing receiver</param>
        /// <param name="item">Item index in the set</param>
        /// <returns>Padded source data from file</returns>
        protected override byte[] GetNextBlockForItemCore(object receiver, int item)
        {
            FileReceiverProperties properties = (FileReceiverProperties)Receivers[receiver];
            ReaderState readerState = properties.ReadStates[item];
            IReaderState fileProperties = readerState;
            
            FileInfo fileInfo = _sources[item];

            if (fileInfo.Stream == null && !fileProperties.ReachedEof)
                fileInfo.Stream = File.OpenRead(fileInfo.FileName);

            // Even for empty files ReachedEof would be true only after one attempt of reading
            // this made especially to allow the algorithms to rotate at least once
            // and crucial in order to make a proper file header
            if (fileProperties.ReachedEof)
                return null;

            byte[] block = new byte[properties.BlockSize];

            fileInfo.Stream.Seek(fileProperties.CurrentPosition, SeekOrigin.Begin);


            int readAmount = fileInfo.Stream.Read(block, 0, block.Length);
            fileProperties.CurrentPosition += readAmount;

            if (readAmount == 0)
            {
                fileProperties.ReachedEof = true;
                bool allReached = true;

                foreach (FileReceiverProperties prop in Receivers.Values)                
                    allReached = allReached && ((IReaderState)prop.ReadStates[item]).ReachedEof;

                if (allReached)
                {
                    fileInfo.Stream.Close();
                    fileInfo.Stream.Dispose();
                    fileInfo.Stream = null;
                }

                return null;
            }

            if (readAmount != properties.BlockSize)
            {
                Array.Resize(ref block, readAmount);

                byte[][] padded = properties.Padding.PadBlock(block, properties.BlockSize);

                for (int i = 0; i < padded.Length; i++)
                    properties.PadedBlocks[item].Enqueue(padded[i]);

                return properties.PadedBlocks[item].Dequeue();
            }



            return block;
        }
    }
}
