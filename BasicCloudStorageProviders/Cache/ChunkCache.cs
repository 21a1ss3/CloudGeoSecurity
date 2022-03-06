using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDepend.Path;

namespace Encryptor.Lib.Cache
{
    public class ChunkCache : ICloudFileChunkCache
    {
        public ChunkCache(int chunkSize) : this(chunkSize, "cache.db")
        {

        }

        public ChunkCache(int chunkSize, string cacheLocation) 
        {
            if (chunkSize < 1)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            if (string.IsNullOrWhiteSpace(cacheLocation))
                throw new ArgumentNullException(nameof(cacheLocation));

            ChunkSize = chunkSize;
            CacheLocation = cacheLocation;

            //if (!cacheLocation.IsValidFilePath())
            //    throw new ArgumentException($"{nameof(cacheLocation)} shall contains proper file path!");

            ExpireTime = new TimeSpan(1 ,0, 0, 0);

            using (DB.ChunkDBContext dbContext = new DB.ChunkDBContext(CacheLocation))
            {
                dbContext.Database.EnsureCreated();
            }
        }

        public int ChunkSize { get; private set; }
        public string CacheLocation { get; private set; }
        public TimeSpan ExpireTime { get; private set; }


        public byte[] TryFetchFromCache(ICloudFileHandle file, long offset)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            DateTime startTime = DateTime.UtcNow - ExpireTime;
            string loweredPath = file.FullPath.ToLower();

            using (DB.ChunkDBContext dbContext = new DB.ChunkDBContext(CacheLocation))
            {

                var dbChunks = from dbFileQI in dbContext.Files
                               join dbChunkQI in dbContext.Chunks on dbFileQI.FileId equals dbChunkQI.FileId
                               where (dbFileQI.CloudId == file.CloudId) && (dbFileQI.FullPath == loweredPath) &&
                                      (dbChunkQI.Offset == offset) && (dbChunkQI.CacheTimestamp >= startTime)
                               select dbChunkQI;

                DB.Chunk foundChunk = null;

                foreach (var dbChunk in dbChunks)
                {
                    if ((foundChunk == null) && !dbChunk.IsDirty)
                        foundChunk = dbChunk;

                    if (dbChunk.IsDirty && (dbChunk.HanldeId == file.HandleId))
                        foundChunk = dbChunk;
                }

                if (foundChunk != null)
                    return foundChunk.Raw;
            }

            return null;
        }

        public void PutInCache(ICloudFileHandle file, long offset, byte[] chunk, bool lastSegment, bool isDirty)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            if (chunk.Length == 0)
                throw new ArgumentException("Chunck with size 0 is meaningless");

            if ((chunk.Length != ChunkSize) && !lastSegment)
                throw new ArgumentException($"Only last chunks could have size less than {nameof(ChunkSize)} property!");
            
            string loweredPath = file.FullPath.ToLower();
            DateTime startTime = DateTime.UtcNow - ExpireTime;

            using (DB.ChunkDBContext dbContext = new DB.ChunkDBContext(CacheLocation))
            {
                DB.File dbFile = dbContext.Files.Where(dbFileQI => 
                                        (dbFileQI.CloudId == file.CloudId) && (dbFileQI.FullPath == loweredPath))
                                        .FirstOrDefault();
                DB.Chunk dbChunk = null;
                bool newChunk = false;


                if (dbFile == null)
                {
                    dbFile = new DB.File();
                    dbFile.CloudId = file.CloudId;
                    dbFile.FullPath = loweredPath;

                    dbContext.Files.Add(dbFile);

                    newChunk = true;
                }

                if (!newChunk)
                    dbChunk = dbContext.Chunks.Where(dbChunkQI =>
                                                        (dbChunkQI.FileId == dbFile.FileId) &&
                                                        (
                                                            (isDirty && dbChunkQI.IsDirty && (dbChunkQI.HanldeId == file.HandleId)) || 
                                                            !dbChunkQI.IsDirty
                                                        ) &&
                                                        (dbChunkQI.Offset == offset) &&
                                                        (dbChunkQI.CacheTimestamp >= startTime)                                                        
                                                    ).FirstOrDefault();

                newChunk = newChunk || (dbChunk == null);

                if (newChunk)
                {
                    dbChunk = new DB.Chunk();

                    dbChunk.CacheTimestamp = DateTime.UtcNow;
                    dbChunk.File = dbFile;
                    dbChunk.Offset = offset;
                    dbChunk.IsLast = lastSegment;

                    if (dbChunk.IsDirty = isDirty)
                        dbChunk.HanldeId = file.HandleId;

                    dbContext.Chunks.Add(dbChunk);
                }


                dbChunk.IsLast = lastSegment;
                dbChunk.Raw = chunk;

                dbContext.SaveChanges();
            }
        }

        public void CutFileLength(ICloudFileHandle file, long newLength)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (newLength < 1)
                throw new ArgumentOutOfRangeException(nameof(newLength));

            string loweredPath = file.FullPath.ToLower();
            long lastChunkOffset = (newLength - 1) / ChunkSize ;

            using (DB.ChunkDBContext dbContext = new DB.ChunkDBContext(CacheLocation))
            {
                var dbChunks = from dbFileQI in dbContext.Files
                               join dbChunkQI in dbContext.Chunks on dbFileQI.FileId equals dbChunkQI.FileId
                               where (dbFileQI.CloudId == file.CloudId) &&
                                     (dbFileQI.FullPath == loweredPath) &&
                                     !dbChunkQI.IsDirty &&
                                     (dbChunkQI.Offset > lastChunkOffset)
                               select dbChunkQI;
                dbContext.RemoveRange(dbChunks);
                dbContext.SaveChanges();
            }
        }

        public void FlushFile(ICloudFileHandle file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            string loweredPath = file.FullPath.ToLower();
            DateTime startTime = DateTime.UtcNow - ExpireTime;

            using (DB.ChunkDBContext dbContext = new DB.ChunkDBContext(CacheLocation))
            {
                var dbChunks = (from dbFileQI in dbContext.Files
                               join dbChunkQI in dbContext.Chunks on dbFileQI.FileId equals dbChunkQI.FileId
                               where (dbFileQI.CloudId == file.CloudId) && 
                                     (dbFileQI.FullPath == loweredPath) &&
                                     dbChunkQI.IsDirty &&
                                     (dbChunkQI.HanldeId == file.HandleId) &&
                                     (dbChunkQI.CacheTimestamp >= startTime)
                               select new { dbChunkQI.FileId, dbChunkQI.Offset, dbChunkQI.ChunkId}).ToArray();

                foreach (var dbChunk in dbChunks)
                {
                    var regularChunk = dbContext.Chunks.Where(dbChunkQI =>
                                                                (dbChunkQI.FileId == dbChunk.FileId) &&
                                                                (dbChunkQI.Offset == dbChunk.Offset) &&
                                                                (!dbChunkQI.IsDirty));

                    DB.Chunk dbChunkWrapper = new DB.Chunk() { ChunkId = dbChunk.ChunkId };
                    dbContext.Chunks.RemoveRange(regularChunk);
                    dbContext.Chunks.Attach(dbChunkWrapper);

                    dbChunkWrapper.IsDirty = false;

                    dbContext.SaveChanges();
                }
            }
        }

    }
}
