using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.GDrive
{
    public class OpenBroswerEventArgs : EventArgs
    {
        public OpenBroswerEventArgs(Uri uriToOpen)
        {
            UriToOpen = uriToOpen ?? throw new ArgumentNullException(nameof(uriToOpen));
        }

        public Uri UriToOpen { get; set; }
    }
}
