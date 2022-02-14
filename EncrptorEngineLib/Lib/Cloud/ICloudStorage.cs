using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public interface ICloudStorage
    {
        public string CloudProviderName { get; }
        public Guid CloudId { get; set; }

        public ICloudItemInfo[] EnumFiles(string path);

        public ICloudItemInfo GetItemInfo(string path);

        public bool FileExists(string path);
        public bool DirectoryExists(string path);

        public void CreateDirectory(string path);

        public byte[] ReadAllBytes(string path);        
        public void WriteAllBytes(string path, byte[] content);
        public byte[] ReadFirstBytes(string path, int size);

        public Stream FileOpenRead(string path);
        public Stream FileOpenWrite(string path);

        public long GetFileSize(string path);
    }
}
