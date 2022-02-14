using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public interface ICloudFileInfo : ICloudItemInfo
    {
        public long FileSize { get; }
    }
}
