using NDepend.Path;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public abstract class CloudStorageBase : ICloudStorage
    {


        private Guid _cloudId;
        private ICloudFileChunkCache _cache;
        private bool _isAuthenticated = false;


        public ICloudFileChunkCache Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }
        public Guid CloudId
        {
            get { return _cloudId; }
            set { _cloudId = value; }
        }
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
            protected set { _isAuthenticated = value; }
        }

        public abstract string CloudProviderName { get; }

        public abstract void CreateDirectory(string path);
        public abstract bool DirectoryExists(string path);
        public abstract ICloudItemInfo[] EnumItems(string path);
        public abstract bool FileExists(string path);
        public abstract Stream FileOpenRead(string path);
        public abstract Stream FileOpenWrite(string path);
        public abstract long GetFileSize(string path);
        public abstract ICloudItemInfo GetItemInfo(string path);
        public abstract byte[] ReadAllBytes(string path);
        public abstract byte[] ReadFirstBytes(string path, int size);
        public abstract void WriteAllBytes(string path, byte[] content);

        protected virtual string NormalisePath(string path)
        {
            if (!path.StartsWith(@".\") || !path.StartsWith(@"./"))
                path = $".{Path.AltDirectorySeparatorChar}{path}";

            return path;
        }

        protected virtual void ValidatePath(string path)
        {
            if (!NormalisePath(path).IsValidRelativeDirectoryPath())
                throw new Exception("The path is invalid!");
        }

        protected string ToPrefixlessRelativePath(string path)
        {
            if (path == ".")
                path = string.Empty;

            if (path.StartsWith(@".\") || path.StartsWith(@"./"))
                path = path.Substring(2);

            path = path.Replace('\\', '/');

            return path;
        }
    }
}
