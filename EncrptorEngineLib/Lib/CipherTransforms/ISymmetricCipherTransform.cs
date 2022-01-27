using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines symmetric cipher transformation
    /// </summary>
    public interface ISymmetricCipherTransform : ICipherTransfrom
    {
        /// <summary>
        /// Gets or sets encryption key
        /// </summary>
        public byte[] Key { get; set; }
        
    }
}
