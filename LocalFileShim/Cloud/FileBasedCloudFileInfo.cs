using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public class FileBasedCloudFileInfo : ICloudFileInfo
    {
        public long FileSize { get; internal set; }

        public string Name { get; internal set; }

        public string FullPath { get; internal set; }

        public string ContainerPath { get; internal set; }
    }
}
