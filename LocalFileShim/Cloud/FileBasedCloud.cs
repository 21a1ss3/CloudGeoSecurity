using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public class FileBasedCloud : ICloudStorage
    {
        public FileBasedCloud(string basePath)
        {
            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            BasePath = basePath;
        }

        public string CloudProviderName => "File-based test cloud provider";

        public Guid CloudId { get; set; }

        private string _basePath;
        private DirectoryInfo _baseDirInfo;
        public string BasePath 
        { 
            get => _basePath; 
            set
            {
                _basePath = value;
                _baseDirInfo = new DirectoryInfo(_basePath);
            }
        }
        

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(Path.Combine(BasePath, path));
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(Path.Combine(BasePath, path));
        }

        public bool FileExists(string path)
        {
            return File.Exists(Path.Combine(BasePath, path));
        }

        public long GetFileSize(string path)
        {
            FileInfo fsFileInfo = new FileInfo(Path.Combine(BasePath, path));

            return fsFileInfo.Length;
        }

        private ICloudFileInfo _createFileInfo(string path)
        {
            FileBasedCloudFileInfo cloudFileInfo = new FileBasedCloudFileInfo();
            FileInfo fsFileInfo = new FileInfo(path);

            cloudFileInfo.ContainerPath = fsFileInfo.DirectoryName.Replace(_baseDirInfo.FullName, "");
            if ((cloudFileInfo.ContainerPath.Length > 0) && (cloudFileInfo.ContainerPath[0] == '\\'))
                cloudFileInfo.ContainerPath = cloudFileInfo.ContainerPath.Substring(1);

            cloudFileInfo.FileSize = fsFileInfo.Length;
            cloudFileInfo.FullPath = Path.TrimEndingDirectorySeparator(fsFileInfo.FullName.Replace(_baseDirInfo.FullName, ""));

            //if (cloudFileInfo.FullPath[0] == '\\')
            //    cloudFileInfo.FullPath = cloudFileInfo.FullPath.Substring(1);

            cloudFileInfo.Name = fsFileInfo.Name;

            return cloudFileInfo;
        }

        private ICloudDirectoryInfo _createDirectoryInfo(string path)
        {
            FileBasedCloudDirectoryInfo cloudDirectoryInfo = new FileBasedCloudDirectoryInfo();
            DirectoryInfo fsDirectoryInfo = new DirectoryInfo(path);

            cloudDirectoryInfo.ContainerPath = fsDirectoryInfo.Parent.FullName.Replace(_baseDirInfo.FullName, "");
            cloudDirectoryInfo.FullPath = fsDirectoryInfo.FullName.Replace(_baseDirInfo.FullName, "");

            if ((cloudDirectoryInfo.ContainerPath.Length > 0) && (cloudDirectoryInfo.ContainerPath[0] == '\\'))
                cloudDirectoryInfo.ContainerPath = cloudDirectoryInfo.ContainerPath.Substring(1);

            if (cloudDirectoryInfo.FullPath[0] == '\\')
                cloudDirectoryInfo.FullPath = cloudDirectoryInfo.FullPath.Substring(1);

            cloudDirectoryInfo.Name = fsDirectoryInfo.Name;

            return cloudDirectoryInfo;
        }


        public ICloudItemInfo[] EnumItems(string path)
        {
            string[] files = Directory.GetFiles(Path.Combine(BasePath, path));
            string[] directories = Directory.GetDirectories(Path.Combine(BasePath, path));

            List<ICloudItemInfo> cloudItems = new List<ICloudItemInfo>(files.Length + directories.Length);

            foreach (var file in files)
                cloudItems.Add(_createFileInfo(file));

            foreach (var directory in directories)
                cloudItems.Add(_createDirectoryInfo(directory));

            return cloudItems.ToArray();
        }

        public Stream FileOpenRead(string path)
        {
            return File.OpenRead(Path.Combine(BasePath, path));
        }

        public Stream FileOpenWrite(string path)
        {
            return File.OpenWrite(Path.Combine(BasePath, path));
        }

        public ICloudItemInfo GetItemInfo(string path)
        {
            ICloudItemInfo info = null;

            if (DirectoryExists(path))
                info = _createDirectoryInfo(Path.Combine(BasePath, path));

            if (FileExists(path))
                info = _createFileInfo(Path.Combine(BasePath, path));

            return info;
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(Path.Combine(BasePath, path));
        }

        public byte[] ReadFirstBytes(string path, int size)
        {
            byte[] buffer;
            using (FileStream fsStream = File.OpenRead(Path.Combine(BasePath, path)))
            {
                buffer = new byte[Math.Min(size, fsStream.Length)];

                int readed = 0;

                do
                    readed += fsStream.Read(buffer, readed, buffer.Length - readed);
                while (readed < buffer.Length);
            }

            return buffer;
        }

        public void WriteAllBytes(string path, byte[] content)
        {
            File.WriteAllBytes(Path.Combine(BasePath, path), content);
        }
    }
}
