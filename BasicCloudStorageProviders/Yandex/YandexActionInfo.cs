using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.Yandex
{
    public class YandexActionInfo : EventArgs
    {
        public YandexActionInfo(Uri webBrowserUri)
        {
            WebBrowserUri = webBrowserUri ?? throw new ArgumentNullException(nameof(webBrowserUri));
            ActionType = YandexActionType.OpenBrowser;
        }

        public YandexActionInfo(string deviceCode, Uri deivceCodeUrl)
        {
            DeviceCode = deviceCode ?? throw new ArgumentNullException(nameof(deviceCode));
            DeivceCodeUrl = deivceCodeUrl ?? throw new ArgumentNullException(nameof(deivceCodeUrl));

            ActionType = YandexActionType.DeviceCodeDisplay;
        }

        public YandexActionType ActionType { get; private set; }
        public Uri WebBrowserUri { get; private set; }
        public Uri DeivceCodeUrl { get; private set; }
        public string DeviceCode { get; private set; }
    }
}
