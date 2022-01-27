using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents dataset no padding for padding alg
    /// </summary>
    public class NoPadding : IDatasetPadding
    {
        /// <summary>
        /// Returns amount of last lines which shall be provided to ClearPaddFromBlocks function 
        /// as a source data if there enough input data.
        /// </summary>
        public int UnpadLinesBatchSize => 0;

        /// <summary>
        /// Perfroms no operation and returns the source array in single line. Desired size is ignored
        /// </summary>
        /// <param name="source">Unpadded source raw bytes</param>
        /// <param name="blockSize">Desired block size</param>
        /// <returns>Wraped in a single line source data</returns>
        public byte[][] PadBlock(byte[] source, ushort blockSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new byte[1][] { source };
        }

        /// <summary>
        /// Perfroms no operation and returns the source array batch. Block size is ignored
        /// </summary>
        /// <param name="source">The source lines of data, each with blockSize to unpad data. See ClearPaddFromBlocks proeprty/</param>
        /// <param name="blockSize">Used block size</param>
        /// <returns>Returned source array without performing any actual transformation</returns>
        public byte[][] ClearPaddFromBlocks(byte[][] source, ushort blockSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source;
        }
    }
}
