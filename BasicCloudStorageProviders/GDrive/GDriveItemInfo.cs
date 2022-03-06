using System;
using System.Collections.Generic;
using System.Text;
using IO = System.IO;
using GDriveData = Google.Apis.Drive.v3.Data;

namespace Encryptor.Lib.GDrive
{
    public abstract class GDriveItemInfo : CloudItemInfo
    {
        protected GDriveItemInfo(GDriveData.File gdriveFile, string folderPath)
        {
            if (gdriveFile == null)
                throw new ArgumentNullException(nameof(gdriveFile));
            if (folderPath == null)
                throw new ArgumentNullException(nameof(folderPath));

            ContainerPath = folderPath.Replace('/', '\\');
            Name = gdriveFile.Name;
            FullPath = IO.Path.Combine(ContainerPath, Name);
        }

    }
}
