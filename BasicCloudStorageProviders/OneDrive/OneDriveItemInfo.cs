using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cnt = Encryptor.Lib.OneDrive.OneDriveConstants;

namespace Encryptor.Lib.OneDrive
{
    public abstract class OneDriveItemInfo : CloudItemInfo
    {

        protected OneDriveItemInfo(DriveItem driveItem)
        {
            if (driveItem == null)
                throw new ArgumentNullException(nameof(driveItem));


            Name = driveItem.Name;
            ContainerPath = driveItem.ParentReference.Path;
            if (ContainerPath.StartsWith(Cnt.RootOneDrvPath))
                ContainerPath = ContainerPath.Substring(Math.Min(Cnt.RootOneDrvPath.Length + 1, ContainerPath.Length));

            ContainerPath = ContainerPath.Replace(Path.AltDirectorySeparatorChar, '\\');

            FullPath = Path.Combine(ContainerPath, Name);
        }

    }
}
