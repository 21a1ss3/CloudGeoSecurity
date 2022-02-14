using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class FileBasedCloudDirectoryInfo : ICloudDirectoryInfo
    {
        public string Name { get; internal set; }

        public string FullPath { get; internal set; }

        public string ContainerPath { get; internal set; }
    }
}
