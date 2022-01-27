using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines sequentable transfrom
    /// </summary>
    public interface ISequenceSetTransform
    {
        /// <summary>
        /// Gets or sets data padding using for current transform's inbound data
        /// </summary>
        public IDatasetPadding Padding { get; set; }

        /// <summary>
        /// Fixes and connects ISequenceSetTransform for specific Dataset and Padding
        /// </summary>
        /// <param name="source">Source dataset</param>
        /// <returns>Connected transfrom, instance of IConnectedTransform</returns>
        public IConnectedTransform ConnectWithDataSet(IDataSet source);
    }
}
