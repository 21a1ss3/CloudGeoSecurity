using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Graph;

namespace Encryptor.Lib.OneDrive
{
    public class OneDriveFileInfo : OneDriveItemInfo, ICloudFileInfo
    {

        public OneDriveFileInfo(DriveItem driveItem)
            :base (driveItem)
        {
            if (driveItem.File == null)
                throw new ArgumentException($"Argument {nameof(driveItem)} shall represent an OneDrive file instance");

            FileSize = driveItem.Size.Value;
        }

        public long FileSize { get; private set; }
    }
}
