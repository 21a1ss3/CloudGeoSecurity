using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.OneDrive
{
    public class InteractiveBrowserCredentialOptionsExtended: InteractiveBrowserCredentialOptions
    {
        public SystemWebViewOptions WebViewOptions { get; set; }
    }
}
