using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public class CloudCachedFileStream : Stream
    {
        public CloudCachedFileStream(ICloudFileHandle file, ICloudFileChunkCache cacheManager)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (cacheManager == null)
                throw new ArgumentNullException(nameof(cacheManager));


            _fileHandle = file;
            _cache = cacheManager;

            _localLength = _fileHandle.Length;
        }

        private ICloudFileHandle _fileHandle;
        private ICloudFileChunkCache _cache;

        public override bool CanRead => !_fileHandle.IsWrite;
        public override bool CanWrite => _fileHandle.IsWrite;
        public override bool CanSeek => true;

        private long _localLength;
        public override long Length => _localLength;

        private long _position = 0;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPostion;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPostion = 0;
                    break;
                case SeekOrigin.Current:
                    newPostion = _position;
                    break;
                case SeekOrigin.End:
                    newPostion = Length;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            newPostion += offset;

            if ((newPostion < 0) || (newPostion > Length))
                throw new ArgumentException("Seeking is attempted before the beginning of the stream.");

            _position = newPostion;

            return newPostion;
        }

        private byte[] _readChunkFromCloud(long chunkInd, bool putInCache = true)
        {
            long chunkStartPos = chunkInd * _cache.ChunkSize;
            long lenToEnd = Length - chunkStartPos;
            byte[] chunk = _fileHandle.FetchRange(chunkStartPos, (int)Math.Min(_cache.ChunkSize, lenToEnd));

            if (putInCache)
                _cache.PutInCache(_fileHandle, chunkInd, chunk, chunk.Length != _cache.ChunkSize, false);

            return chunk;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if ((offset < 0) || (offset >= buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            if ((count < 0) || (buffer.Length < (offset + count)))
                throw new ArgumentOutOfRangeException(nameof(count));

            if (!CanRead)
                throw new InvalidOperationException("Read operation is unsupported for this stream");

            if (count == 0)
                return 0;

            if (_position >= Length)
                return 0;

            long endPosition = Math.Min(_localLength, _position + count);
            long startChunk = _position / _cache.ChunkSize;
            long chunksCount = (endPosition - 1) / _cache.ChunkSize + 1;
            int chunkOffset = (int)(_position % _cache.ChunkSize);
            int chunkSize = _cache.ChunkSize - chunkOffset;


            int read = 0;

            for (int i = 0; i < chunksCount; i++)
            {
                byte[] chunk = _cache.TryFetchFromCache(_fileHandle, startChunk + i);

                // If cache miss
                if (chunk == null)
                    chunk = _readChunkFromCloud (startChunk + i);
                

                int actualCount = Math.Min(chunkSize, count);

                Array.Copy(chunk, chunkOffset, buffer, offset, actualCount);

                _position += actualCount;
                offset += actualCount;
                read += actualCount;
                count -= actualCount;

                chunkOffset = 0;
                chunkSize = (int) Math.Min(_cache.ChunkSize, Length - Position);
            }

            return read;
        }


        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (!CanWrite)
                throw new InvalidOperationException("Read-only streams does not support changing of the length");

            _localLength = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if ((offset < 0) || (offset >= buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            if ((count < 0) || (buffer.Length < (offset + count)))
                throw new ArgumentOutOfRangeException(nameof(count));

            if (!CanWrite)
                throw new InvalidOperationException("Write operation is unsupported for this stream");

            if (count == 0)
                return;

            long endPosition = _position + count;
            long startChunk = _position / _cache.ChunkSize;
            long chunksCount = (endPosition - 1) / _cache.ChunkSize + 1;
            int chunkOffset = (int)(_position % _cache.ChunkSize);
            int chunkSize = Math.Min(_cache.ChunkSize - chunkOffset, count);

            long localPosition = _position;

            for (long i = 0; i < chunksCount; i++)
            {
                byte[] chunk = null;

                if (chunkSize != _cache.ChunkSize)
                {
                    chunk = _cache.TryFetchFromCache(_fileHandle, startChunk + i);

                    // In case of cache miss and if we in boundance of previous file
                    if ((chunk == null) && (localPosition < _fileHandle.Length))
                        chunk = _readChunkFromCloud(startChunk + i);

                    if ((chunk != null) && (chunk.Length < (chunkOffset + chunkSize)))
                    {
                        byte[] newChunk = new byte[chunkOffset + chunkSize];

                        Array.Copy(chunk, 0, newChunk, 0, chunk.Length);

                        chunk = newChunk;
                    }
                }

                if (chunk == null)
                    chunk = new byte[chunkSize];

                Array.Copy(buffer, offset, chunk, chunkOffset, chunkSize);

                _cache.PutInCache(_fileHandle, startChunk + i, chunk, (i + 1) == chunksCount, true);
                
                localPosition += chunkSize;
                offset += chunkSize;
                count -= chunkSize;

                chunkOffset = 0;
                chunkSize = Math.Min(_cache.ChunkSize, count);
            }

            _localLength = Math.Max(Length, endPosition);
            _position = localPosition;
        }


        public override void Flush()
        {
            if (!CanWrite)
                throw new InvalidOperationException("Flush is unsupported for this stream");

            _cache.FlushFile(_fileHandle);
            
            long chunksCount = (Length - 1) / _cache.ChunkSize + 1;
            long localPosition = 0;
            long remainLen = Length;

            using (Stream serverStream = _fileHandle.SendFileAsync(Length).GetAwaiter().GetResult())
            {
                for (long i = 0; i < chunksCount; i++)
                {
                    long chunkSize = Math.Min(_cache.ChunkSize, remainLen);
                    byte[] chunk = _cache.TryFetchFromCache(_fileHandle, i);

                    if ((chunk == null) && (localPosition < _fileHandle.Length))
                        chunk = _readChunkFromCloud(i, false);
                    
                    if (chunk == null)
                        chunk = new byte[chunkSize];

                    serverStream.Write(chunk, 0, chunk.Length);

                    localPosition += _cache.ChunkSize;
                    remainLen -= _cache.ChunkSize;

                    if ((i > 0) && ((i & 1) == 0))
                        serverStream.Flush();
                }

                serverStream.Flush();
                serverStream.Close();
            }

            _cache.CutFileLength(_fileHandle, _localLength);
        }
    }
}
