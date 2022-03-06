using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using NDepend.Path;

namespace Encryptor.Lib.Cache.DB
{
    public class ChunkDBContext : DbContext
    {
        public ChunkDBContext(string cacheDbLocation)
        {
            if(string.IsNullOrWhiteSpace(cacheDbLocation))
                throw new ArgumentNullException(nameof(cacheDbLocation));

            CacheDbLocation = cacheDbLocation;

        }

        public string CacheDbLocation { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={CacheDbLocation}");

        }

        public DbSet<File> Files { get; set; }
        public DbSet<Chunk> Chunks { get; set; }
    }
}
