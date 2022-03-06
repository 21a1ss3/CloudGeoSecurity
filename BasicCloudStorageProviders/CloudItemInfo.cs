using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public abstract class CloudItemInfo : ICloudItemInfo
    {
        public virtual string Name { get; protected set; }

        public virtual string FullPath { get; protected set; }

        public virtual string ContainerPath { get; protected set; }
    }
}
