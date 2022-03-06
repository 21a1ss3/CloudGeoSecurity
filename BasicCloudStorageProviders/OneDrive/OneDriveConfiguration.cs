using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.OneDrive
{
    public class OneDriveConfiguration
    {
        public string AppId { get; set; }
        public string Tenant { get; set; } = "common";
        //public string Secret { get; set; }
    }
}
