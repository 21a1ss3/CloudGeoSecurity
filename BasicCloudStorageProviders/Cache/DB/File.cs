using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Encryptor.Lib.Cache.DB
{
    public class File
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FileId { get; set; }
        public Guid CloudId { get; set; }
        public string FullPath { get; set; }

        public IList<Chunk> Chunks { get; set; } = new List<Chunk>();
    }
}
