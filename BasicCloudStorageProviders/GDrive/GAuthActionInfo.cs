using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.GDrive
{
    public class GAuthActionInfo : EventArgs
    {
        public GAuthActionInfo(Uri webBrowserUri)
        {
            WebBrowserUri = webBrowserUri ?? throw new ArgumentNullException(nameof(webBrowserUri));

            ActionType = GAuthActionType.LaunchBrowser;
        }

        public GAuthActionType ActionType { get; set; }
        public Uri WebBrowserUri { get; set; }
    }
}
