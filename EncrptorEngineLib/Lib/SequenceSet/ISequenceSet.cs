using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines a sequence set of transformations
    /// </summary>
    public interface ISequenceSet
    {
        /// <summary>
        /// Schedules transformation in the set
        /// </summary>
        /// <param name="transform">Transform whether to schedule</param>
        /// <returns>Called ISequenceSet object (this) for flow API</returns>
        public ISequenceSet ScheduleTransformation(ISequenceSetTransform transform);

        /// <summary>
        /// Launching the tranformartion of the source data
        /// </summary>
        /// <param name="source">Dataset containing the source data</param>
        /// <returns>Connected sequence set of transformations to a specific source data set</returns>
        public IConnectedTransform StartTransformation(IDataSet source);

        /// <summary>
        /// Gets count of scheduled transformation
        /// </summary>
        public int Length { get; }
    }
}
