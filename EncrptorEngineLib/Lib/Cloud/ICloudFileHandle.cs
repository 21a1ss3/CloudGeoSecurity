using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Encryptor.Lib
{
    public interface ICloudFileHandle
    {
        public Guid CloudId { get; }
        public Guid HandleId { get; }
        public string FileName { get; }
        public string FullPath { get; }
        public long Length { get; }
        public bool Exist { get; }
        public bool IsWrite { get; }

        public byte[] FetchRange(long offset, int size);
        public Task<Stream> SendFileAsync(long newLength);
    }
}
