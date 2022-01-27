using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines a cipher transformation
    /// </summary>
    public interface ICipherTransfrom: ISequenceSetTransform
    {
        /// <summary>
        /// Gets algorithm name
        /// </summary>
        public string AlgorigthName { get; }
        /// <summary>
        /// Gets or sets encryption mode
        /// </summary>
        public bool Encrypt { get; set; }
    }
}
