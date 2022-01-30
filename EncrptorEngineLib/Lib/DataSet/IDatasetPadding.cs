using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents source data padding to align it to blocksize
    /// </summary>
    public interface IDatasetPadding
    {
        /// <summary>
        /// Prefroms padding source data to reach specific block size. Shall be called at last block of data stream
        /// </summary>
        /// <param name="source">Unpadded source raw bytes</param>
        /// <param name="blockSize">Desired block size</param>
        /// <returns>
        /// Padded source raw bytes lines up to length of blockSize for each line.
        /// In cases if function does no padding the result data could have size which would be less 
        /// than desired blocksize!
        /// </returns>
        public byte[][] PadBlock(byte[] source, ushort blockSize);

        /// <summary>
        /// Performs backward operation, if possible, of padding.
        /// </summary>
        /// <param name="source">The source lines of data, each with blockSize to unpad data. See ClearPaddFromBlocks proeprty/</param>
        /// <param name="blockSize">Used block size</param>
        /// <returns>Cleared raw bytes</returns>
        public byte[][] ClearPadFromBlocks(byte[][] source, ushort blockSize);

        /// <summary>
        /// Returns amount of last lines which shall be provided to ClearPaddFromBlocks function 
        /// as a source data if there enough input data.
        /// </summary>
        public int UnpadLinesBatchSize { get; }
    }
}
