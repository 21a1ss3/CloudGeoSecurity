using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Holds memory dataset receiver properties
    /// </summary>
    public class QueuedReceiverProperties : DataSetBase.ReceiverProperties
    {
        /// <summary>
        /// Represent single queue for single item per one receiver
        /// </summary>
        public class QueueState
        {
            private byte[] _buffer;

            /// <summary>
            /// Initialises new instance of QueueState with given block size and padding
            /// </summary>
            /// <param name="size">Block size</param>
            /// <param name="padding">Padding</param>
            public QueueState(ushort size, IDatasetPadding padding)
            {
                Buffer = new byte[size];
                Written = 0;
                Padding = padding ?? throw new ArgumentNullException(nameof(padding));
            }

            /// <summary>
            /// Gets the queue with ready to read buffers
            /// </summary>
            public Queue<byte[]> ReadyBuffers { get; private set; } = new Queue<byte[]>();
            /// <summary>
            /// Gets current in-work buffer
            /// </summary>
            public byte[] Buffer { get => _buffer; private set => _buffer = value; }
            /// <summary>
            /// Gets or sets amount of written data in current buffer
            /// </summary>
            public int Written { get; set; }
            /// <summary>
            /// Gets using padding for align data
            /// </summary>
            public IDatasetPadding Padding { get; private set; }
            /// <summary>
            /// Gets or sets reaching end of buffer
            /// </summary>
            public bool Finished { get; set; } = false;

            /// <summary>
            /// Forcebly flushes buffer
            /// </summary>
            /// <param name="finish">
            /// true for forcebly flush empty buffer at the moment of reaching end of buffer position 
            /// and trigger proper padding handle, otherwise - false
            /// </param>
            public void FlushBuffer(bool finish = false)
            {
                if ((Written > 0) || finish)
                {
                    int len = Buffer.Length;
                    if (finish)
                    {
                        Array.Resize(ref _buffer, Written);
                        byte[][] padded = Padding.PadBlock(Buffer, (ushort)len);

                        foreach (byte[] padLine in padded)
                            ReadyBuffers.Enqueue(padLine);
                    }
                    else
                        ReadyBuffers.Enqueue(Buffer);
                    Buffer = new byte[len];
                    Written = 0;
                }
            }

            /// <summary>
            /// Flush buffer if it is full
            /// </summary>
            public void FlushOnFullBuffer()
            {
                if (Buffer.Length == Written)
                    FlushBuffer();
            }
        }

        /// <summary>
        /// Contains all receivers buffers queue
        /// </summary>
        public List<QueueState> ReceiveQueue { get; private set; } = new List<QueueState>();
    }
}
