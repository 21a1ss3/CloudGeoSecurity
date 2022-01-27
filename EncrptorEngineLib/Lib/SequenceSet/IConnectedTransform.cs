using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines connected to the source sequentable transformation
    /// </summary>
    public interface IConnectedTransform
    {
        /// <summary>
        /// Gets source dataset
        /// </summary>
        public IDataSet Source { get; }
        /// <summary>
        /// Gets tranformed dataset
        /// </summary>
        public IDataSet Result { get; }
        /// <summary>
        /// Performs transformation of single block of data for each items in source data set
        /// </summary>
        /// <returns>true if there available more blocks to transform, otherwise - false</returns>
        public bool TransformNext();
        /// <summary>
        /// Specifies whether there is more blocks to transform.
        /// </summary>
        public bool MoreAvailable { get; }
    }
}
