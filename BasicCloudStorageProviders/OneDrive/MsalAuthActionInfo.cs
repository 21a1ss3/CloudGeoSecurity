using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.OneDrive
{
    public class MsalAuthActionInfo : EventArgs
    {
        public MsalAuthActionInfo(string deviceCode, Uri visitUrl)
        {
            DeviceCode = deviceCode ?? throw new ArgumentNullException(nameof(deviceCode));
            DeviceCodeVisitUrl = visitUrl ?? throw new ArgumentNullException(nameof(visitUrl));

            MsalAuthAction = MsalAuthActionKind.DeviceCodeDisplay;
        }

        public MsalAuthActionInfo(Uri authUrl)
        {
            WebBrowserUrl = authUrl ?? throw new ArgumentNullException(nameof(authUrl));

            MsalAuthAction = MsalAuthActionKind.OpenBrowser;
        }

        public MsalAuthActionKind MsalAuthAction { get; private set; }

        public string DeviceCode { get; private set; }
        public Uri DeviceCodeVisitUrl { get; private set; }

        public Uri WebBrowserUrl { get; set; }
    }
}
