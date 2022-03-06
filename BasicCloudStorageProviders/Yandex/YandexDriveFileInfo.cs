using Encryptor.Lib.Yandex.HttpUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.Yandex
{
    public class YandexDriveFileInfo : YandexDriveItemInfo, ICloudFileInfo
    {
        public YandexDriveFileInfo(YandexSdkDriveItem driveItem) : base(driveItem)
        {
            if (driveItem.IsDirectory)
                throw new ArgumentException($"{nameof(driveItem)} shall represent a file!");

            FileSize = driveItem.ContentLength;           
        }

        public long FileSize { get; private set; }
    }
}
