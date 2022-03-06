using System;
using System.Collections.Generic;
using System.Text;
using GDriveData = Google.Apis.Drive.v3.Data;

namespace Encryptor.Lib.GDrive
{
    public class GDriveDirectoryInfo : GDriveItemInfo, ICloudDirectoryInfo
    {
        public GDriveDirectoryInfo(GDriveData.File gdriveFile, string folderPath)
            :base (gdriveFile, folderPath)
        {
            if (!gdriveFile.MimeType.Contains("application/vnd.google-apps.folder"))
                throw new ArgumentException($"{nameof(gdriveFile)} shall be a folder");
        }
    }
}
