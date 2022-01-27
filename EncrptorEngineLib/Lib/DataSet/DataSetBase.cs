using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Implements basic behaviour of data set
    /// </summary>
    public abstract class DataSetBase : IDataSet
    {
        /// <summary>
        /// Initializes a new instance of the DataSetBase with given amount of items in the set
        /// </summary>
        /// <param name="count">Amount of items in the dataset, or null if it is not established</param>
        protected DataSetBase(int? count)
        {
            Count = count;
            _init();
        }

        /// <summary>
        /// Initializes a new instance of the DataSetBase for new chained dataset with identical amount of items as in previous data set.
        /// </summary>
        /// <param name="previousDataSet">Previous dataset</param>
        protected DataSetBase(IDataSet previousDataSet)
        {
            Count = previousDataSet.Count;

            _previousDataSet = previousDataSet ?? throw new ArgumentNullException(nameof(previousDataSet));

            _init();
        }

        /// <summary>
        /// Holds a reference to a previous dataset, if it is present
        /// </summary>
        private IDataSet _previousDataSet;
        /// <summary>
        /// Holds the list of handlers of Count determination event
        /// </summary>
        private List<Action<IDataSet>> _countChangeHandlers;

        /// <summary>
        /// Backing field of Count property
        /// </summary>
        private int? _count;

        /// <summary>
        /// Returns or (protected) sets the amount of items within data set. 
        /// In case if total amount is not established, this property will return null.
        /// The property value could be updated once when it contains null and will trigger calling of all registred
        /// count determination handlers
        /// </summary>
        public int? Count
        {
            get { return _count; }
            protected set
            {
                if (_count != null)
                    throw new InvalidOperationException("Count can be changed once when it's not determined!");

                if (value != null)
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("Count shall be only non-negative number");

                _count = value;

                OnCountDetermined(Count.Value);

                if (_count != null && _countChangeHandlers != null)
                {
                    foreach (var hndlr in _countChangeHandlers)
                        hndlr(this);

                    _countChangeHandlers = null;
                }
            }
        }

        /// <summary>
        /// Returns read-only dictionary contained receiver properties for each receiver
        /// </summary>
        public ReadOnlyDictionary<object, ReceiverProperties> Receivers { get; private set; }

        /// <summary>
        /// Providing a delegate to function which is subscribing handler on count determination.
        /// </summary>
        public Action<Action<IDataSet>> RegisterCountEstablishedHandler { get; private set; }
        private List<Type> _recieverPropertyTypes = new List<Type>();
        private Type _topRecieverPropertyType = typeof(ReceiverProperties);
        private Dictionary<object, ReceiverProperties> _receivers = new Dictionary<object, ReceiverProperties>();
        /// <summary>
        /// Holds a referrence to a single insatnce of empty array
        /// </summary>
        public static byte[] EmptyArray { get; private set; } = new byte[0];

        /// <summary>
        /// Returns a next block to transform for specific item in set
        /// </summary>
        /// <param name="receiver">An object representing receiver</param>
        /// <param name="item">Item index in the set</param>
        /// <returns> - A byte[BlockSize] or [1..Blocksize] block of source bytes bases on padding
        ///           - byte[0] when bufers isn't full
        ///           - null when reached EOF</returns>
        public byte[] GetNextBlockForItem(object receiver, int item)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));

            if (Count == null)
                throw new InvalidOperationException("Count shall be determined!");

            if ((0 > item) || (item > Count))
                throw new ArgumentOutOfRangeException(nameof(item));

            if (!Receivers.ContainsKey(receiver))
                throw new ArgumentException($"Receiver shall be registred first");

            ReceiverProperties properties = Receivers[receiver];

            if (properties.PadedBlocks.Length == 0)
                ((IReceiverProperties)properties).ResizePaddedQueues(Count.Value);

            if (properties.PadedBlocks[item].Count > 0)
                return properties.PadedBlocks[item].Dequeue();

            return GetNextBlockForItemCore(receiver, item);
        }

        /// <summary>
        /// Internal call for derived classesfrom GetNextBlockForItem
        /// </summary>
        /// <param name="reciever">Verified recevier</param>
        /// <param name="item">Verified item index</param>
        /// <returns>Same as GetNextBlockForItem</returns>
        protected abstract byte[] GetNextBlockForItemCore(object reciever, int item);


        /// <summary>
        /// Performs registration of the reciever side with preffered blocksize and padding. Consider to use NoPadding in backward direction
        /// </summary>
        /// <typeparam name="T">Any receiver class. T is not accepting any value type for preventing boxing behaviour</typeparam>
        /// <param name="receiver">An object representing receiver</param>
        /// <param name="blockSize">Block size in bytes</param>
        /// <param name="padding">The used padding scheme</param>
        public void RegisterReceiver<T>(T receiver, ushort blockSize, IDatasetPadding padding) where T : class
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));

            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            if (_receivers.ContainsKey(receiver))
                throw new InvalidOperationException("This reciever already registred");

            if (blockSize < 1)
                throw new ArgumentOutOfRangeException($"{nameof(blockSize)} could not be less than 1");

            ReceiverProperties props = _createReceiverPropInstance();
            ((IReceiverProperties)props).SetBlockSize(blockSize);
            ((IReceiverProperties)props).SetPadding(padding);

            OnBeforeRegisterReceiver(receiver, props);
            _receivers.Add(receiver, props);
            OnAfterRegisterReceiver(receiver, props);
        }


        /// <summary>
        /// Invoked before receiver will be registred. 
        /// Note: use in the constructor RequireRecieverPropertyType for providing custom type for receiver properties
        /// </summary>
        /// <param name="receiver">Registring receiver</param>
        /// <param name="properties">Instance of properties</param>
        protected virtual void OnBeforeRegisterReceiver(object receiver, ReceiverProperties properties)
        {

        }

        /// <summary>
        /// Invoked after receiver was being registred. 
        /// </summary>
        /// <param name="receiver">Registring receiver</param>
        /// <param name="properties">Instance of properties</param>
        protected virtual void OnAfterRegisterReceiver(object receiver, ReceiverProperties properties)
        {

        }

        /// <summary>
        /// Allocating new instance of receiver properties
        /// </summary>
        /// <returns></returns>
        private ReceiverProperties _createReceiverPropInstance()
        {
            return (ReceiverProperties)_topRecieverPropertyType.GetConstructor(new Type[0]).Invoke(null);
        }

        /// <summary>
        /// Provides opportunity to claim derived class for receiver's properties.
        /// After call this method perfroms checks against all previously claimed datatypes in order to make sure
        /// that all intermediate class are satisfied as well.
        /// This method must be used only in the constructor or initialisation methods not more than 
        /// once per each derived class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected void RequireRecieverPropertyType<T>() where T: ReceiverProperties, new()
        {
            Type newPropType = typeof(T);

            foreach (Type prevType in _recieverPropertyTypes)
                if (!newPropType.IsSubclassOf(prevType))
                    throw new Exception($"The class \"{newPropType.FullName}\" shall be derived from \"{prevType.FullName}\" class");

            _recieverPropertyTypes.Add(newPropType);
            _topRecieverPropertyType = newPropType;
        }

        /// <summary>
        /// Perfroms initial common initialisation between multiple constructors
        /// </summary>
        private void _init()
        {
            if (Count == null)
            {
                if (_previousDataSet != null)
                {
                    RegisterCountEstablishedHandler = _previousDataSet.RegisterCountEstablishedHandler;
                    RegisterCountEstablishedHandler((sender) =>
                    {
                        Count = _previousDataSet.Count;
                    });
                }
                else
                {
                    _countChangeHandlers = new List<Action<IDataSet>>();
                    RegisterCountEstablishedHandler = _registerCountEstablishedHandler;
                }
            }

            Receivers = new ReadOnlyDictionary<object, ReceiverProperties>(_receivers);
        }

        /// <summary>
        /// Invoked when Count property has been determined
        /// </summary>
        /// <param name="newCount"></param>
        protected virtual void OnCountDetermined(int newCount)
        {

        }

        /// <summary>
        /// Invoked to collect callbacks of Count determination handlers
        /// </summary>
        /// <param name="handler"></param>
        private void _registerCountEstablishedHandler(Action<IDataSet> handler)
        {
            _countChangeHandlers.Add(handler);
        }

        /// <summary>
        /// Internal interface, used to hide some setters of ReceiverProperties
        /// </summary>
        private interface IReceiverProperties
        {
            public void SetBlockSize(ushort size);
            public void SetPadding(IDatasetPadding padding);
            public void ResizePaddedQueues(int newSize);
        }

        /// <summary>
        /// Represent receiver's state property information
        /// </summary>
        public class ReceiverProperties : IReceiverProperties
        {
            /// <summary>
            /// Contains queue of padded blocks
            /// </summary>
            private Queue<byte[]>[] _padedBlocks = new Queue<byte[]>[0];


            /// <summary>
            /// Gets desired size of one line (block) of source data.
            /// </summary>
            public ushort BlockSize { get; private set; }
            /// <summary>
            /// Returns an instance of receiver padding
            /// </summary>
            public IDatasetPadding Padding { get; private set; }
            /// <summary>
            /// Returns the queue of padded blocks for each item
            /// </summary>
            public Queue<byte[]>[] PadedBlocks => _padedBlocks;


            
            void IReceiverProperties.SetBlockSize(ushort size)
            {
                BlockSize = size;
            }

            void IReceiverProperties.SetPadding(IDatasetPadding padding)
            {
                Padding = padding;
            }

            /// <summary>
            /// Allocates new pad queues per each item in data set
            /// </summary>
            /// <param name="newSize">Amount of items in dataset</param>
            void IReceiverProperties.ResizePaddedQueues(int newSize)
            {
                Array.Resize(ref _padedBlocks, newSize);

                for (int i = 0; i < _padedBlocks.Length; i++)
                    if (_padedBlocks[i] == null)
                        _padedBlocks[i] = new Queue<byte[]>();
            }
        }
    }
}
