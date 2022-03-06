using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using IO = System.IO;
using GDriveData = Google.Apis.Drive.v3.Data;
using NDepend.Path;
using Google.Apis.Upload;

namespace Encryptor.Lib.GDrive
{
    public class GoogleDriveCloud : CloudStorageBase
    {

        public GoogleDriveCloud()
            : this(0)
        {

        }

        public GoogleDriveCloud(int configIndex)
        {
            if (configIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            var appConfig = new ConfigurationBuilder()
                            .AddUserSecrets<GoogleDriveCloud>()
                            .Build();

            List<GoogleDriveConfiguration> configs = appConfig.GetSection("Clouds:GoogleDrive")
                                                    .Get<List<GoogleDriveConfiguration>>();

            if (configs == null)
                throw new Exception("No configuration has been found for Google Drive apps");

            if (configs.Count <= configIndex)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            _config = configs[configIndex];

            if (string.IsNullOrWhiteSpace(_config.ClientId))
                throw new ArgumentNullException("Google Drive client id must be configured!");
        }

        private static readonly string[] _scopes = { DriveService.ScopeConstants.DriveFile };


        private GoogleDriveConfiguration _config;
        private UserCredential _credential;
        private DriveService _apiClient;



        public override string CloudProviderName => "Google Drive";
        public event EventHandler<GAuthActionInfo> AuthentificationAction;



        public bool SignIn(GAuthOptions options)
        {
            ICodeReceiver codeReceiver = null;

            switch (options)
            {
                case GAuthOptions.Browser:
                    break;
                case GAuthOptions.CustomBrowser:
                    var customReceiver = new CustomBrowserLocalCodeReceiver();
                    codeReceiver = customReceiver;

                    customReceiver.LaunchBrowser += CustomReceiver_LaunchBrowser;

                    break;
                default:
                    throw new ArgumentException($"Unsupported {nameof(GAuthOptions)} option!");
            }

            var clientSecret = new ClientSecrets();
            clientSecret.ClientId = _config.ClientId;
            clientSecret.ClientSecret = _config.ClientSecret;

            try
            {
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecret, _scopes, _config.UserId, default, codeReceiver: codeReceiver)
                                .ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (Console.Error != null)
                    Console.Error.WriteLine($"Error in fetching token: {ex}");
                return false;
            }

            _apiClient = new DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Cloud Data Security"
            });

            IsAuthenticated = true;

            return true;
        }

        private void CustomReceiver_LaunchBrowser(object sender, OpenBroswerEventArgs e)
        {
            AuthentificationAction?.Invoke(this, new GAuthActionInfo(e.UriToOpen));
        }

        protected bool IsDirectory(GDriveData.File file) => file?.MimeType.Contains("application/vnd.google-apps.folder")??throw new ArgumentNullException(nameof(file));

        protected ResolvedPathResult ResolvePath(string path)
        {
            ValidatePath(path);

            path = ToPrefixlessRelativePath(path);

            if ((path.Length > 0) && (path[path.Length - 1] == '/'))
                path = path.Substring(0, path.Length - 1);


            ResolvedPathResult result = new ResolvedPathResult();
            result.UsedPath = path;
            bool isFile = false;

            if (string.IsNullOrWhiteSpace(path))
                return result;

            string[] pathSegments = path.Split('/');
            string nextParentId = result.ParentId;

            foreach (var pathSegement in pathSegments)
            {
                if (isFile)
                    return null;

                var nameCond = $"name = '{pathSegement}'";
                var parentCond = $"'{nextParentId}' in parents";
                var req = _apiClient.Files.List();

                req.Q = $"{nameCond} and {parentCond} and mimeType = 'application/vnd.google-apps.folder' and not trashed";
                req.Fields = "files(id, name, parents, mimeType, size, trashed)";
                GDriveData.File file = null;

                //try
                //{
                var files = req.Execute().Files;

                if (files.Count > 0)
                    file = files[0];
                //}
                //catch (Exception ex)
                //{

                //}

                if (file == null)
                {
                    req.Q = $"{nameCond} and {parentCond} and mimeType != 'application/vnd.google-apps.folder' and not trashed";
                    files = req.Execute().Files;

                    if (files.Count > 0)
                        file = files[0];

                    isFile = true;
                }
                

                if (file == null)
                    return null;

                Console.WriteLine($"Path: {path}, segment: {pathSegement}, parentId: {string.Join(", ", file.Parents)}, isFile: {isFile}");

                result.GDriveFile = file;
                result.ParentId = nextParentId;
                nextParentId = file.Id;
            }

            return result;
        }


        public override void CreateDirectory(string path)
        {
            ValidatePath(path);

            var relativePath = NormalisePath(path).ToRelativeDirectoryPath();
            string parentDir = relativePath.ParentDirectoryPath.ToStringOrIfNullToEmptyString();

            var resolvedParentPath = ResolvePath(parentDir);

            if (resolvedParentPath == null)
                throw new Exception("Unable to found parent directory!");

            if ((resolvedParentPath.GDriveFile != null) && (!IsDirectory(resolvedParentPath.GDriveFile)))
                throw new Exception("Parent path shall point to a directory!");

            GDriveData.File newdir = new GDriveData.File();

            newdir.Name = relativePath.DirectoryName;
            newdir.Parents = new string[] { resolvedParentPath.GDriveFile?.Id?? "root" };
            newdir.MimeType = "application/vnd.google-apps.folder";


            _apiClient.Files.Create(newdir).Execute();
        }

        public override bool DirectoryExists(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                return false;

            return (resolvedPath.GDriveFile == null) || IsDirectory(resolvedPath.GDriveFile);
        }

        public override ICloudItemInfo GetItemInfo(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                return null;

            // root folder
            if (resolvedPath.GDriveFile == null)
                return new GDriveDirectoryInfo(new GDriveData.File() { Name = string.Empty }, string.Empty);

            if (IsDirectory(resolvedPath.GDriveFile))
                return new GDriveDirectoryInfo(resolvedPath.GDriveFile, resolvedPath.UsedPath);
            else
                return new GDriveFileInfo(resolvedPath.GDriveFile, resolvedPath.UsedPath);
        }

        public override ICloudItemInfo[] EnumItems(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                throw new Exception("Path not found");

            string pageToken = null;

            List<ICloudItemInfo> items = new List<ICloudItemInfo>();

            do
            {
                var req = _apiClient.Files.List();
                req.Q = $"'{resolvedPath.GDriveFile?.Id??"root"}' in parents and not trashed";
                req.Fields = "nextPageToken, files(id, name, mimeType, size)";
                req.PageToken = pageToken;

                var filesPage = req.Execute();

                foreach (var gdriveFile in filesPage.Files)
                {
                    if (IsDirectory(gdriveFile))
                        items.Add(new GDriveDirectoryInfo(gdriveFile, resolvedPath.UsedPath));
                    else
                        items.Add(new GDriveFileInfo(gdriveFile, resolvedPath.UsedPath));
                }

                pageToken = filesPage.NextPageToken;
            } while (pageToken != null);

            return items.ToArray();
        }

        public override bool FileExists(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            return (resolvedPath != null) && ((resolvedPath.GDriveFile != null) && !IsDirectory(resolvedPath.GDriveFile));
        }


        public override long GetFileSize(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                throw new Exception("Item has not been found");

            if ((resolvedPath.GDriveFile == null) || IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");

            return resolvedPath.GDriveFile.Size.Value;
        }

        public override byte[] ReadAllBytes(string path)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);
            
            if (resolvedPath == null)
                throw new Exception("Item has not been found");

            if ((resolvedPath.GDriveFile == null) || IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");

            if (resolvedPath.GDriveFile.Size.Value > int.MaxValue)
                throw new Exception("File to big to be read. Use FileOpenRead");

            IO.MemoryStream readStream = new IO.MemoryStream((int)resolvedPath.GDriveFile.Size.Value);
            var progress = _apiClient.Files.Get(resolvedPath.GDriveFile.Id).DownloadWithStatus(readStream);

            if (progress.Status == Google.Apis.Download.DownloadStatus.Failed)
                throw new Exception("Faield to download file", progress.Exception);
                      
            return readStream.ToArray();
        }

        public override byte[] ReadFirstBytes(string path, int size)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                throw new Exception("Item has not been found");

            if ((resolvedPath.GDriveFile == null) || IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");

            if (resolvedPath.GDriveFile.Size.Value > int.MaxValue)
                throw new Exception("File to big to be read. Use FileOpenRead");

            int actualSize = Math.Min((int)resolvedPath.GDriveFile.Size.Value, size);

            IO.MemoryStream readStream = new IO.MemoryStream(actualSize);
            var progress = _apiClient.Files.Get(resolvedPath.GDriveFile.Id)
                                            .DownloadRange(readStream, new System.Net.Http.Headers.RangeHeaderValue(0, actualSize - 1));

            if (progress.Status == Google.Apis.Download.DownloadStatus.Failed)
                throw new Exception("Faield to download file", progress.Exception);

            return readStream.ToArray();
        }

        public override void WriteAllBytes(string path, byte[] content)
        {
            ValidatePath(path);

            var resolvedPath = ResolvePath(path);
            var relativePath = NormalisePath(path).ToRelativeFilePath();
            var resolvedParentPath = ResolvePath(relativePath.ParentDirectoryPath.ToString());


            if (resolvedParentPath == null)
                throw new Exception("Parent diectory does not exist!");

            if ((resolvedParentPath.GDriveFile != null) && (!IsDirectory(resolvedParentPath.GDriveFile)))
                throw new Exception("Parent path shall point towards directory!");

            if ((resolvedPath?.GDriveFile != null) && IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");


            GDriveData.File file = resolvedPath?.GDriveFile;

            if (file == null)
                file = new GDriveData.File()
                {
                    Name = relativePath.FileName,
                    Parents = new string[] { resolvedParentPath.GDriveFile.Id }
                };

            var progress = _apiClient.Files.Create(file, new IO.MemoryStream(content), "application/octet-stream")
                    .Upload();

            if (progress.Status == UploadStatus.Failed)
                throw new Exception("Faield to upload file", progress.Exception);
        }


        public override IO.Stream FileOpenRead(string path)
        {
            ValidatePath(path);

            if (Cache == null)
                throw new Exception("You must configure cache first");

            var resolvedPath = ResolvePath(path);

            if (resolvedPath == null)
                throw new Exception("Item has not been found");

            if ((resolvedPath.GDriveFile == null) || IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");

            GDriveFileHandle handle = new GDriveFileHandle(_apiClient,
                                                           resolvedPath.GDriveFile.Id,
                                                           resolvedPath.GDriveFile.Name,
                                                           resolvedPath.ParentId,
                                                           IO.Path.Combine(resolvedPath.UsedPath, resolvedPath.GDriveFile.Name),
                                                           resolvedPath.GDriveFile.Size.Value,
                                                           false,
                                                           CloudId);

            return new CloudCachedFileStream(handle, Cache);
        }

        public override IO.Stream FileOpenWrite(string path)
        {
            ValidatePath(path);

            if (Cache == null)
                throw new Exception("You must configure cache first");

            var resolvedPath = ResolvePath(path);
            var resolvedParentPath = ResolvePath(NormalisePath(path).ToRelativeFilePath().ParentDirectoryPath.ToString());

            if (resolvedParentPath == null)
                throw new Exception("Parent diectory does not exist!");

            if ((resolvedParentPath.GDriveFile != null) && (!IsDirectory(resolvedParentPath.GDriveFile)))
                throw new Exception("Parent path shall point towards directory!");            

            if ((resolvedPath?.GDriveFile != null) && IsDirectory(resolvedPath.GDriveFile))
                throw new Exception("Specified path points onto a directory");
                                 

            string fileId = resolvedPath?.GDriveFile?.Id;
            string fileName = resolvedPath?.GDriveFile?.Name ?? NormalisePath(path).ToRelativeFilePath().FileName;
            string parentId = resolvedParentPath.GDriveFile.Id ?? "root";
            string fullPath = IO.Path.Combine(resolvedParentPath.UsedPath, fileName);
            long length = resolvedPath?.GDriveFile?.Size ?? 0;


            GDriveFileHandle handle = new GDriveFileHandle(_apiClient,
                                                           fileId,
                                                           fileName,
                                                           parentId,
                                                           fullPath,
                                                           length,
                                                           true,
                                                           CloudId);

            return new CloudCachedFileStream(handle, Cache);
        }

        protected class ResolvedPathResult
        {
            public string ParentId { get; set; } = "root";
            public GDriveData.File GDriveFile { get; set; }
            public string UsedPath { get; set; }
        }
    }
}
