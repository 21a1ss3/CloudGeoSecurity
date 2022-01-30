using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents padding scheme with 0. This scheme is undoibale
    /// </summary>
    public class ZeroPadding : IDatasetPadding
    {
        /// <summary>
        /// Returns amount of last lines which shall be provided to ClearPaddFromBlocks function 
        /// as a source data if there enough input data.
        /// </summary>
        public int UnpadLinesBatchSize =>0;

        /// <summary>
        /// Pads the source data with 0s to reach desired size
        /// </summary>
        /// <param name="source">Unpadded source raw bytes</param>
        /// <param name="blockSize">Desired block size</param>
        /// <returns>Padded source raw bytes in single line</returns>
        public byte[][] PadBlock(byte[] source, ushort blockSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (blockSize < source.Length)
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            if (source.Length == 0)
                return new byte[1][] { source };

            byte[] padded = new byte[blockSize];

            Array.Copy(source, 0, padded, 0, source.Length);

            return new byte[1][] { padded };
        }

        /// <summary>
        /// This operation is not supported and shall be never called. Throws System.NotSupportedException.
        /// </summary>
        /// <param name="source">The source lines of data, each with blockSize to unpad data. See ClearPaddFromBlocks proeprty/</param>
        /// <param name="blockSize">Used block size</param>
        /// <returns>Thorws exception. See details.</returns>
        public byte[][] ClearPadFromBlocks(byte[][] source, ushort blockSize)
        {
            throw new NotSupportedException();
        }
    }
}
