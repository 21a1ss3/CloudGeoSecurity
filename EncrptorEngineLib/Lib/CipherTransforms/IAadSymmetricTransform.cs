using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    /// <summary>
    /// Defines a symmetric cipher tranformation with Additional Authenticated Data (AAD)
    /// </summary>
    public interface IAadSymmetricTransform : ISymmetricCipherTransform
    {
        /// <summary>
        /// Gets or sets public data to authenticate
        /// </summary>
        public byte[] PublicData { get; set; }
    }
}
