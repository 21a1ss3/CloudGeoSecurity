using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.OneDrive
{
    public class OneDriveDirectoryInfo : OneDriveItemInfo, ICloudDirectoryInfo
    {
        public OneDriveDirectoryInfo(DriveItem driveItem)
            :base(driveItem)
        {
            if (driveItem.Folder == null)
                throw new ArgumentException($"Argument {nameof(driveItem)} shall represent an OneDrive folder instance");
        }
    }
}
