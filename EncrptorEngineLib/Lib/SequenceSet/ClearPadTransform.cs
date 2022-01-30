using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Implements removing padding in backward directions as transformation
    /// </summary>
    public class ClearPadTransform : ISequenceSetTransform
    {
        /// <summary>
        /// Represents implementation IConnectedTransform for ClearPadTransform
        /// </summary>
        private class _unpadConnectedTransform : IConnectedTransform
        {
            /// <summary>
            /// Represents a container for cached last N lines required for specific padding scheme
            /// </summary>
            private class _sourceState
            {
                internal _sourceState(int lines)
                {
                    Queue = new Queue<byte[]>(lines + 1);
                }

                public Queue<byte[]> Queue { get; private set; }
                public bool IsPadded { get; set; } = true;
            }

            /// <summary>
            /// Initialises a new instance of connected transform
            /// </summary>
            /// <param name="sourceSet">The source data set</param>
            /// <param name="padding">Padding</param>
            /// <param name="blockSize">Used desired size of single block for padding</param>
            internal _unpadConnectedTransform(IDataSet sourceSet, IDatasetPadding padding, ushort blockSize)
            {
                Source = sourceSet;
                _resultTransform = new InMemoryDataset(sourceSet);
                _padding = padding;
                _blockSize = blockSize;
                _linesInBatch = padding.UnpadLinesBatchSize;

                Source.RegisterReceiver(this, blockSize, new NoPadding());
            }


            private IDatasetPadding _padding;
            private InMemoryDataset _resultTransform;
            private ushort _blockSize;
            private _sourceState[] _cachedLines;
            private int _linesInBatch;

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
            public IDataSet Result => _resultTransform;

            /// <summary>
            /// Performs transformation of single block of data for each items in source data set
            /// </summary>
            /// <returns>true if there available more blocks to transform, otherwise - false</returns>
            public bool TransformNext()
            {
                if (Source.Count == null)
                    throw new InvalidOperationException("Count shall be determined first!");

                if (_cachedLines == null)
                    _cachedLines = new _sourceState[Source.Count.Value];

                bool more = false;

                for (int i = 0; i < Source.Count; i++)
                {
                    if (_cachedLines[i] == null)
                        _cachedLines[i] = new _sourceState(_linesInBatch);

                    byte[] source = Source.GetNextBlockForItem(this, i);
                    byte[] result = new byte[0];

                    if (source == null)
                    {
                        if (_cachedLines[i].Queue.Count == 0)
                            result = null;
                        else
                        {
                            if (_cachedLines[i].IsPadded)
                            {
                                byte[][] lines = _cachedLines[i].Queue.ToArray();
                                _cachedLines[i].Queue.Clear();

                                byte[][] unpadded = _padding.ClearPadFromBlocks(lines, _blockSize);

                                foreach (byte[] unpadLine in unpadded)
                                    _cachedLines[i].Queue.Enqueue(unpadLine);

                                _cachedLines[i].IsPadded = false;
                            }

                            result = _cachedLines[i].Queue.Dequeue();
                        }
                    }
                    else
                    {
                        if (source.Length != 0)
                            _cachedLines[i].Queue.Enqueue(source);

                        if (_cachedLines[i].Queue.Count > _linesInBatch)
                            result = _cachedLines[i].Queue.Dequeue();
                    }

                    if (result != null)
                    {
                        more = true;
                        _resultTransform.WriteToBuffer(i, result);
                    }
                    else
                        _resultTransform.FinishItem(i);
                }

                MoreAvailable = more;

                return more;
            }
        }

        /// <summary>
        /// Gets or sets data padding using for current transform's outbound data
        /// </summary>
        public IDatasetPadding Padding { get; set; }

        /// <summary>
        /// Gets or sets used desired size of one line (block) of source data.
        /// </summary>
        public ushort BlockSize { get; set; }

        /// <summary>
        /// Fixes and connects ClearPadTransform for specific Dataset and Padding
        /// </summary>
        /// <param name="source">Source dataset</param>
        /// <returns>Connected transfrom, instance of IConnectedTransform</returns>
        public IConnectedTransform ConnectWithDataSet(IDataSet source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (Padding == null)
                throw new Exception($"{nameof(Padding)} property must be configured first!");

            if (BlockSize < 1)
                throw new Exception($"{nameof(BlockSize)} property must be configured first and be positive!");


            return new _unpadConnectedTransform(source, Padding, BlockSize);
        }
    }
}
