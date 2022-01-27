using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents RAM-based dataset, like System.IO.MemoryStream
    /// </summary>
    public class InMemoryDataset : DataSetBase
    {
        /// <summary>
        /// Initialises a new RAM-based dataset instance
        /// </summary>
        public InMemoryDataset() : this(count: null)
        {
            RequireRecieverPropertyType<QueuedReceiverProperties>();
        }

        /// <summary>
        /// Initialises a new RAM-based dataset instance for specific amount of items
        /// </summary>
        /// <param name="count">Amount of items in the dataset, or null if it is not established</param>
        public InMemoryDataset(int? count) : base(count)
        {
            RequireRecieverPropertyType<QueuedReceiverProperties>();
            _initQueues();
        }

        /// <summary>
        /// Initializes a new RAM-based dataset instance for new chained dataset with identical amount of items as in previous data set.
        /// </summary>
        /// <param name="previousDataSet">Previous dataset</param>
        public InMemoryDataset(IDataSet previousDataSet)
            : base(previousDataSet)
        {
            RequireRecieverPropertyType<QueuedReceiverProperties>();
            _initQueues();
        }

        /// <summary>
        /// Once can be used to determine count of items
        /// </summary>
        /// <param name="count"></param>
        public void SetCount(int count)
        {
            Count = count;
            _initQueues();
        }

        /// <summary>
        /// Internal initialize mehtod of receiver queues
        /// </summary>
        private void _initQueues()
        {
            if ((Count != null) && (_finishedItems == null))
            {
                _finishedItems = new bool[Count.Value];

                for (int i = 0; i < _finishedItems.Length; i++)
                    _finishedItems[i] = false;

                foreach (QueuedReceiverProperties qProps in Receivers.Values)
                {
                    for (int i = 0; i < Count; i++)
                        qProps.ReceiveQueue.Add(new QueuedReceiverProperties.QueueState(qProps.BlockSize, qProps.Padding));
                }
            }
        }

        private bool[] _finishedItems;

        /// <summary>
        /// Writing data from sender to multiple receivers in other side
        /// </summary>
        /// <param name="item">Written item index</param>
        /// <param name="buffer">Raw data as byte array</param>
        public void WriteToBuffer(int item, byte[] buffer)
        {
            if (Count == null)
                throw new InvalidOperationException("Count shall be determined!");

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if ((0 > item) || (item > Count))
                throw new ArgumentOutOfRangeException(nameof(item));

            if (_finishedItems[item])
                throw new InvalidOperationException("Cannot write in finished item");


            foreach (QueuedReceiverProperties receiver in Receivers.Values)
            {
                QueuedReceiverProperties.QueueState state = receiver.ReceiveQueue[item];

                int taken = 0;
                while (taken < buffer.Length)
                {
                    int length = Math.Min(buffer.Length - taken, receiver.BlockSize - state.Written);

                    Array.Copy(buffer, taken, state.Buffer, state.Written, length);
                    state.Written += length;
                    taken += length;

                    state.FlushOnFullBuffer();
                }
            }
        }

        /// <summary>
        /// Marks specific item as finished and flushes all buffers
        /// </summary>
        /// <param name="item">Finisged item index</param>
        public void FinishItem(int item)
        {
            if (Count == null)
                throw new InvalidOperationException("Count shall be determined!");

            if ((0 > item) || (item > Count))
                throw new ArgumentOutOfRangeException(nameof(item));

            if (!_finishedItems[item])
            {
                _finishedItems[item] = true;
                foreach (QueuedReceiverProperties receiver in Receivers.Values)
                    receiver.ReceiveQueue[item].FlushBuffer(true);
            }
        }

        /// <summary>
        /// Checks if there item is finished
        /// </summary>
        /// <param name="item">Item index to check</param>
        /// <returns>true if item is finished, false - otherwise</returns>
        public bool IsFinished(int item)
        {
            if (Count == null)
                throw new InvalidOperationException("Count shall be determined!");

            if ((0 > item) || (item > Count))
                throw new ArgumentOutOfRangeException(nameof(item));

            return _finishedItems[item];
        }

        /// <summary>
        /// Overrides DataSetBase.OnBeforeRegisterReceiver and configures receiver properties
        /// </summary>
        /// <param name="receiver">Registring receiver</param>
        /// <param name="properties">Instance of properties</param>
        protected override void OnBeforeRegisterReceiver(object receiver, ReceiverProperties properties)
        {
            base.OnBeforeRegisterReceiver(receiver, properties);

            QueuedReceiverProperties qProps = (QueuedReceiverProperties)properties;

            if (Count != null)
                for (int i = 0; i < Count; i++)
                    qProps.ReceiveQueue.Add(new QueuedReceiverProperties.QueueState(qProps.BlockSize, qProps.Padding));
        }

        /// <summary>
        /// Overrides DataSetBase.GetNextBlockForItemCore and returning requested source data from the memory buffer.
        /// </summary>
        /// <param name="receiver">An object representing receiver</param>
        /// <param name="item">Item index in the set</param>
        /// <returns>Padded source data from memory buffer</returns>
        protected override byte[] GetNextBlockForItemCore(object reciever, int item)
        {
            QueuedReceiverProperties properties = (QueuedReceiverProperties)Receivers[reciever];

            if (properties.ReceiveQueue[item].Finished)
                return null;

            var state = properties.ReceiveQueue[item];

            if (state.ReadyBuffers.Count > 0)
                return state.ReadyBuffers.Dequeue();

            if (_finishedItems[item])
            {
                properties.ReceiveQueue[item].Finished = true;
                return null;
            }

            return EmptyArray;
        }

    }
}
