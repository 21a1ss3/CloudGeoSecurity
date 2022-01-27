using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Represents a sequence set of transformations
    /// </summary>
    public class SequenceSet : ISequenceSet
    {
        /// <summary>
        /// Represents connected to source dataset sequence set of transfromations
        /// </summary>
        private class ConnectedTransform : IConnectedTransform
        {
            internal ConnectedTransform(List<ISequenceSetTransform> sequence, IDataSet source)
            {
                Source = source;
                Result = source;
                _sequence = new IConnectedTransform[sequence.Count];

                for (int i = 0; i < _sequence.Length; i++)
                {
                    _sequence[i] = sequence[i].ConnectWithDataSet(Result);
                    Result = _sequence[i].Result;
                }
            }

            private IConnectedTransform[] _sequence;

            /// <summary>
            /// Gets source dataset
            /// </summary>
            public IDataSet Source { get; private set; }
            /// <summary>
            /// Gets tranformed dataset
            /// </summary>
            public IDataSet Result { get; private set; }
            /// <summary>
            /// Specifies whether there is more blocks to transform.
            /// </summary>
            public bool MoreAvailable { get; private set; }

            /// <summary>
            /// Gets count of scheduled transformation on moment of connection
            /// </summary>
            public int Length => _sequence.Length;

            /// <summary>
            /// Performs transformation of single block of data for each items in source data set
            /// </summary>
            /// <returns>true if there available more blocks to transform, otherwise - false</returns>
            public bool TransformNext()
            {
                bool more = false;

                foreach (var transform in _sequence)
                    more = transform.TransformNext() || more;

                MoreAvailable = more;

                return more;
            }
        }

        private List<ISequenceSetTransform> _tranforms = new List<ISequenceSetTransform>();

        /// <summary>
        /// Gets count of scheduled transformation
        /// </summary>
        public int Length => _tranforms.Count;

        /// <summary>
        /// Schedules transformation in the set
        /// </summary>
        /// <param name="transform">Transform whether to schedule</param>
        /// <returns>Called SequenceSet object (this) for flow API</returns>
        public SequenceSet ScheduleTransformation(ISequenceSetTransform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (_tranforms.Contains(transform))
                return this;

            _tranforms.Add(transform);

            return this;
        }

        ISequenceSet ISequenceSet.ScheduleTransformation(ISequenceSetTransform transform) 
            => ScheduleTransformation(transform);

        /// <summary>
        /// Launching the tranformartion of the source data
        /// </summary>
        /// <param name="source">Dataset containing the source data</param>
        /// <returns>Connected sequence set of transformations to a specific source data set</returns>
        public IConnectedTransform StartTransformation(IDataSet source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ConnectedTransform(_tranforms, source);
        }
    }
}
