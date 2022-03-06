using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib
{
    public interface ICloudFileChunkCache
    {
        public int ChunkSize { get; }

        public void PutInCache(ICloudFileHandle file, long offset, byte[] chunk, bool lastSegment, bool isDirty);
        public byte[] TryFetchFromCache(ICloudFileHandle file, long offset);
        public void FlushFile(ICloudFileHandle file);
        public void CutFileLength(ICloudFileHandle file, long newLength);
    }
}
