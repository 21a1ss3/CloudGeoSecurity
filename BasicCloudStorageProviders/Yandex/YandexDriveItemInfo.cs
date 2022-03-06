using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Encryptor.Lib.Yandex
{
    public abstract class YandexDriveItemInfo : CloudItemInfo
    {
        protected YandexDriveItemInfo(HttpUtils.YandexSdkDriveItem driveItem)
        {
            if (driveItem == null)
                throw new ArgumentNullException(nameof(driveItem));


            Name = driveItem.DisplayName;
            string[] pathParts = driveItem.FullPath.Split('/',StringSplitOptions.RemoveEmptyEntries);
            FullPath = string.Join('\\', pathParts);
            ContainerPath = string.Join('\\', pathParts.SkipLast(1));            
        }
    }
}
