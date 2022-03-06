using System;
using System.Collections.Generic;
using System.Text;
using GDriveData = Google.Apis.Drive.v3.Data;

namespace Encryptor.Lib.GDrive
{
    public class GDriveFileInfo : GDriveItemInfo, ICloudFileInfo
    {
        public GDriveFileInfo(GDriveData.File gdriveFile, string folderPath)
            : base(gdriveFile, folderPath)
        {
            if (gdriveFile.MimeType.Contains("application/vnd.google-apps.folder"))
                throw new ArgumentException($"{nameof(gdriveFile)} shall be a file");

            FileSize = gdriveFile.Size.Value;
        }

        public long FileSize { get; protected set; }
    }
}
