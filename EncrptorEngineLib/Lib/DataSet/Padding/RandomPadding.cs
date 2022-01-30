using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents padding scheme with random bytes
    /// </summary>
    public class RandomPadding : IDatasetPadding
    {
        /// <summary>
        /// Returns amount of last lines which shall be provided to ClearPaddFromBlocks function 
        /// as a source data if there enough input data.
        /// </summary>
        public int UnpadLinesBatchSize => 2;

        /// <summary>
        /// Pads the source data to desired size with random data and random-based length to provide abillity to remove the padding
        /// </summary>
         /// <param name="source">The source lines of data, each with blockSize to unpad data. See ClearPaddFromBlocks proeprty/</param>
        /// <param name="blockSize">Used block size</param>
        /// <returns>One or two lines of padded data, each with blockSize length</returns>
        public byte[][] PadBlock(byte[] source, ushort blockSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if ((blockSize < source.Length) || (blockSize < 2))
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            int maxPadSize = blockSize - 1;
            byte lines = 1;

            if (source.Length >= maxPadSize)
                lines++;

            byte[][] result = new byte[lines][];

            for (int i = 0; i < result.Length; i++)
                result[i] = new byte[blockSize];

            source.CopyTo(result[0], 0);

            int padIndex = source.Length;
            int totalLength = blockSize * lines;
            ushort padLength = (ushort)(totalLength - 2 - padIndex);
            ushort randomisedPadLength;

            do
            {
                randomisedPadLength = (ushort)(RandomNumberGenerator.GetInt32(ushort.MaxValue + 1));
                randomisedPadLength -= (ushort)(padLength % maxPadSize);
                randomisedPadLength += padLength;
            } while (randomisedPadLength % blockSize != padLength); //Protecting of overflow without loosing the range

            using (RandomNumberGenerator crNumGen = RandomNumberGenerator.Create())
            {
                bool writeMainBytes = true;

                for (int seq = 0;
                    padIndex < totalLength;
                    padIndex++, seq++)
                {
                    int line = padIndex / blockSize;
                    int col = padIndex % blockSize;

                    if (writeMainBytes)
                    {
                        if (col != (blockSize - 2))
                            crNumGen.GetBytes(result[line], padIndex, 1);
                        else
                        {
                            result[line][col] = (byte)((randomisedPadLength >> 8) & 0xFF);
                            writeMainBytes = false;
                        }
                    }
                    else
                        result[line][col] = (byte)(randomisedPadLength & 0xFF);
                }
            }

            Console.WriteLine($"{nameof(RandomPadding)}: source size: {source.Length}; padding: {padLength}; randomised: {randomisedPadLength}; blockSize: {blockSize}");

            return result;
        }

        /// <summary>
        /// Clears the padding from input data
        /// </summary>
        /// <param name="source">Two last lines of source data with blockSize, or one if input source have no more data</param>
        /// <param name="blockSize">Used block size</param>
        /// <returns>Cleared single line of raw bytes</returns>
        public byte[][] ClearPadFromBlocks(byte[][] source, ushort blockSize)
        {
            if ((source == null) || (source.Length == 0))
                throw new ArgumentNullException(nameof(source));

            if ((blockSize < source.Length) || (blockSize < 2))
                throw new ArgumentOutOfRangeException(nameof(blockSize));
            
            int maxPadSize = blockSize - 1;
            ushort padLentgh;
            int totalLength = blockSize * source.Length;
            byte[] lastLine = source[source.Length - 1];
            ushort rawPadLen;

            rawPadLen = padLentgh = (ushort)((lastLine[lastLine.Length - 2] << 8) | lastLine[lastLine.Length - 1]);

            padLentgh %= blockSize;

            if ((padLentgh == maxPadSize) && (source.Length == 1))
                throw new Exception("No enough lines are provided for such padding");

            totalLength -= padLentgh + 2;
            int realLines = (totalLength - 1) / blockSize + 1;
            byte[][] result = new byte[realLines][];

            for (int i = 0; i < realLines; i++)
            {
                result[i] = new byte[Math.Min(blockSize, totalLength)];
                Array.Copy(source[i], 0, result[i], 0, result[i].Length);
                totalLength -= result[i].Length;
            }

            Console.WriteLine($"{nameof(RandomPadding)}: source size: {result[realLines-1].Length}; padding: {padLentgh}; randomised: {rawPadLen}; blockSize: {blockSize}");

            return result;
        }
    }
}
