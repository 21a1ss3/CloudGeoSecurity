using Encryptor.Lib.Yandex.HttpUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.Yandex
{
    public class YandexDirectoryInfo : YandexDriveItemInfo, ICloudDirectoryInfo
    {
        public YandexDirectoryInfo(YandexSdkDriveItem driveItem) : base(driveItem)
        {
            if (!driveItem.IsDirectory)
                throw new ArgumentException($"{nameof(driveItem)} shall represent a directory!");
        }
    }
}
