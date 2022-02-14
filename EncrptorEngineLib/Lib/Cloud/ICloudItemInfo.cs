using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public interface ICloudItemInfo
    {
        public string Name { get; }
        public string FullPath { get; }
        public string ContainerPath { get; }
    }
}
