using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents a data set of several data items and spilts them into blocks
    /// </summary>
    public interface IDataSet
    {
        /// <summary>
        /// Returns total count of data streams in this data set or null if it currenlty impossible to establish
        /// </summary>
        public int? Count { get; }

        /// <summary>
        /// Performs registration of the reciever side with preffered blocksize and padding.
        /// Consider to use NoPadding in backward direction
        /// </summary>
        /// <typeparam name="T">Any receiver class. T is not accepting any value type for preventing boxing behaviour</typeparam>
        /// <param name="receiver">An object representing receiver</param>
        /// <param name="blockSize">Block size in bytes</param>
        /// <param name="padding">The used padding scheme</param>
        public void RegisterReceiver<T>(T receiver, ushort blockSize, IDatasetPadding padding) where T : class;

        /// <summary>
        /// Returns a next block to transform for specific item in set
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="item">Item index in the set</param>
        /// <returns> - A byte[BlockSize] or [1..Blocksize] block of source bytes bases on padding
        ///           - byte[0] when buffers isn't full
        ///           - null when reached EOF</returns>
        public byte[] GetNextBlockForItem(object receiver, int item);

        /*
         * This option with a property was used to make around
         * recursive calls and free stack frames. This is useful
         * in case when you have multiple tranformations and
         * some tranfromation is unable to establish a proper
         * amount of streams before begin. In this case the first
         * dataset in undefined chain is defining a delegate to 
         * handler signup function and the rest are just copying it
         * and by that preventing recursion to occur.
         */
        /// <summary>
        /// Providing a delegate to function which is subscribing handler on count determination.
        /// </summary>
        public Action<Action<IDataSet>> RegisterCountEstablishedHandler { get; }
    }
}
