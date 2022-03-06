using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Encryptor.Lib.Cache.DB
{
    public class Chunk
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChunkId { get; set; }
        public int FileId { get; set; }
        public long Offset { get; set; }
        public byte[] Raw { get; set; }
        public bool IsLast { get; set; }

        public Guid? HanldeId { get; set; }
        public bool IsDirty { get; set; }

        public DateTime CacheTimestamp { get; set; }

        [ForeignKey("FileId")]
        public File File { get; set; }
    }
}
