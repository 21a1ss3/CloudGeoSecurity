using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using NDepend.Path;
using Cnt = Encryptor.Lib.OneDrive.OneDriveConstants;

namespace Encryptor.Lib.OneDrive
{
    public class OneDriveCloud : CloudStorageBase
    {

        public OneDriveCloud()
            : this(0)
        {

        }

        public OneDriveCloud(int configIndex)
        {
            if (configIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            var appConfig = new ConfigurationBuilder()
                            .AddUserSecrets<OneDriveCloud>()
                            .Build();

            List<OneDriveConfiguration> configs = appConfig.GetSection("Clouds:OneDrive")
                                                    .Get<List<OneDriveConfiguration>>();

            if (configs == null)
                throw new Exception("No configuration has been found for OneDrive apps");

            if (configs.Count <= configIndex)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            _config = configs[configIndex];

            if (string.IsNullOrWhiteSpace(_config.AppId))
                throw new ArgumentNullException("OneDrive application (client) id must be configured!");

            _apiClient = new GraphServiceClient(authenticationProvider: null);
            _context = new TokenRequestContext(_scopes, tenantId: _config.Tenant);
        }

        public bool SignIn(MsalAuthOption option)
        {
            TokenCredential credential;

            switch (option)
            {
                case MsalAuthOption.ConsoleDeviceCode:
                    credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions()
                    {
                        DeviceCodeCallback = (code, _) =>
                                     {
                                         AuthentificationAction?.Invoke(this,
                                             new MsalAuthActionInfo(code.UserCode, code.VerificationUri));

                                         return Task.CompletedTask;
                                     },
                    });
                    break;
                case MsalAuthOption.EmbededBrowser:
                    credential = new InteractiveBrowserCredentialExtended(new InteractiveBrowserCredentialOptionsExtended()
                    {
                        ClientId = _config.AppId,
                        WebViewOptions = new Microsoft.Identity.Client.SystemWebViewOptions()
                        {
                            OpenBrowserAsync = (url) =>
                                { 
                                    AuthentificationAction?.Invoke(this,
                                             new MsalAuthActionInfo(url));

                                    return Task.CompletedTask;
                                }
                        }
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option));
            }

            _apiClient.AuthenticationProvider = new TokenCredentialAuthProvider(credential, _scopes);

            try
            {
                AccessToken _token = credential.GetToken(_context, CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (Console.Error != null)
                    Console.Error.WriteLine($"Error in fetching token: {ex}");
                return false;
            }

            IsAuthenticated = true;

            return true;
        }

        private static readonly string[] _scopes = {"Files.Read", "Sites.Read.All", "Files.ReadWrite", "Files.ReadWrite.All", 
                                                    "Files.ReadWrite.AppFolder", "Files.ReadWrite.Selected", "Sites.Read.All", "Sites.ReadWrite.All", 
                                                    "Sites.ReadWrite.All", "User.Read" }; //, "Sites.Read.Selected"


        private GraphServiceClient _apiClient;
        private OneDriveConfiguration _config;
        private TokenRequestContext _context;




        public event EventHandler<MsalAuthActionInfo> AuthentificationAction;


        public override string CloudProviderName => "OneDrive";
        
        protected virtual IDriveRequestBuilder GetDriveBuilder() => _apiClient.Me.Drive;

        protected IDriveItemRequestBuilder BuildItemRequestByPath(string path)
        {
            var itemReqBuilder = GetDriveBuilder().Root;
            var oneDrivePath = ToPrefixlessRelativePath(path);

            if (!string.IsNullOrWhiteSpace(oneDrivePath))
                itemReqBuilder = itemReqBuilder.ItemWithPath(oneDrivePath);

            return itemReqBuilder;
        }


        public override void CreateDirectory(string path)
        {
            ValidatePath(path);

            var relativePath = NormalisePath(path).ToRelativeDirectoryPath();
            string parentDir = relativePath.ParentDirectoryPath.ToStringOrIfNullToEmptyString();

            BuildItemRequestByPath(parentDir).Children.Request().AddAsync(new DriveItem()
            {
                Folder = new Folder(),
                Name = relativePath.DirectoryName,
            }).GetAwaiter().GetResult();
        }

        public override bool DirectoryExists(string path)
        {
            ValidatePath(path);

            try
            {
                var item = BuildItemRequestByPath(path).Request().GetAsync().GetAwaiter().GetResult();

                return item.Folder != null;
            }
            catch
            {

            }

            return false;
        }

        public override bool FileExists(string path)
        {
            ValidatePath(path);

            try
            {
                var item = BuildItemRequestByPath(path).Request().GetAsync().GetAwaiter().GetResult();

                return item.File != null;
            }
            catch
            {

            }

            return false;
        }

        public override ICloudItemInfo GetItemInfo(string path)
        {
            ValidatePath(path);

            try
            {
                var item = BuildItemRequestByPath(path).Request().GetAsync().GetAwaiter().GetResult();

                if (item.File != null)
                    return new OnedriveFileInfo(item);

                if (item.Folder != null)
                    return new OnedriveDirectoryInfo(item);
            }
            catch
            {

            }

            return null;
        }

        public override long GetFileSize(string path)
        {
            ValidatePath(path);

            var item = BuildItemRequestByPath(path).Request().GetAsync().GetAwaiter().GetResult();

            if (item.File != null)
                return item.Size.Value;

            throw new Exception("File has been not found");
        }

        public override ICloudItemInfo[] EnumItems(string path)
        {
            ValidatePath(path);
            
            var remoteListOfFiles = BuildItemRequestByPath(path).Children.Request().GetAsync().GetAwaiter().GetResult();
            List<ICloudItemInfo> items = new List<ICloudItemInfo>();

            while (remoteListOfFiles != null)
            {
                foreach (var driveItem in remoteListOfFiles)
                {
                    if (driveItem.File != null)
                        items.Add(new OnedriveFileInfo(driveItem));

                    if (driveItem.Folder != null)
                        items.Add(new OnedriveDirectoryInfo(driveItem));
                }

                remoteListOfFiles = remoteListOfFiles.NextPageRequest?.GetAsync().GetAwaiter().GetResult();
            }

            return items.ToArray();
        }



        public override byte[] ReadAllBytes(string path)
        {
            ValidatePath(path);

            var driveItemReq = BuildItemRequestByPath(path);
            var driveItem = driveItemReq.Request().GetAsync().GetAwaiter().GetResult();

            if (driveItem.File == null)
                throw new Exception("Requested item is not a file");

            //if (!driveItem.AdditionalData.ContainsKey("@microsoft.graph.downloadUrl"))
            //    throw new Exception("Unable to resolve download url for the file");

            //string url = (string)driveItem.AdditionalData["@microsoft.graph.downloadUrl"];
            if (driveItem.Size.Value > int.MaxValue)
                throw new Exception("File to big to be read. Use FileOpenRead");

            byte[] rawData = new byte[driveItem.Size.Value];
            int readed = 0;


            var driveItemContent = driveItemReq.Content.Request().GetAsync().GetAwaiter().GetResult();

            using (Stream contentStream = driveItemContent)
            {
                while (readed < rawData.LongLength)
                    readed += contentStream.Read(rawData, readed, rawData.Length - readed);

                contentStream.Close();
            }

            return rawData;
        }


        public override void WriteAllBytes(string path, byte[] content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            ValidatePath(path);

            var contentRequestBuilder = BuildItemRequestByPath(path).Content.Request();

            contentRequestBuilder.Method = HttpMethods.PUT;
            var httpRequest = contentRequestBuilder.GetHttpRequestMessage();

            httpRequest.Content = new ByteArrayContent(content);
            httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = contentRequestBuilder.Client.HttpProvider.SendAsync(httpRequest).GetAwaiter().GetResult();

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
                throw new Exception($"Remote endpoint declined request. Response: {response.StatusCode:D} {response.ReasonPhrase}");
        }





        public override byte[] ReadFirstBytes(string path, int size)
        {
            ValidatePath(path);

            var driveItemReq = BuildItemRequestByPath(path);
            var driveItem = driveItemReq.Request().GetAsync().GetAwaiter().GetResult();

            if (driveItem.File == null)
                throw new Exception("Requested item is not a file");

            //if (!driveItem.AdditionalData.ContainsKey("@microsoft.graph.downloadUrl"))
            //    throw new Exception("Unable to resolve download url for the file");

            //string url = (string)driveItem.AdditionalData["@microsoft.graph.downloadUrl"];

            byte[] rawData = new byte[Math.Min(driveItem.Size.Value, size)];
            int readed = 0;

            var driveItemContent = driveItemReq.Content.Request().GetAsync().GetAwaiter().GetResult();

            using (Stream contentStream = driveItemContent)
            {
                while (readed < rawData.Length)
                    readed += contentStream.Read(rawData, readed, (int)rawData.Length - readed);
            }

            return rawData;
        }



        public override Stream FileOpenRead(string path)
        {
            ValidatePath(path);

            if (Cache == null)
                throw new Exception("You must configure cache first");

            ICloudFileInfo fileInfo = GetItemInfo(path) as ICloudFileInfo;

            if ((fileInfo == null))
                throw new Exception($"File with {nameof(path)} not found or it is pointing not to a file!");

            var reqBuilder = BuildItemRequestByPath(path);

            OneDriveFileHandle handle = new OneDriveFileHandle(
                                                                CloudId,
                                                                fileInfo.Name,
                                                                fileInfo.FullPath, 
                                                                fileInfo.FileSize,
                                                                exist: true, 
                                                                isWrite: false, 
                                                                reqBuilder);

            return new CloudCachedFileStream(handle, Cache);
        }

        public override Stream FileOpenWrite(string path)
        {
            ValidatePath(path);

            if (Cache == null)
                throw new Exception("You must configure cache first");

            ICloudItemInfo itemInfo = GetItemInfo(path);
            ICloudFileInfo fileInfo = itemInfo as ICloudFileInfo;

            if ((itemInfo != null) && (fileInfo == null))
                throw new Exception($"{nameof(path)} must points onto file!");

            long length = fileInfo?.FileSize ?? 0;
            string fileName = fileInfo?.Name ?? NormalisePath(path).ToRelativeFilePath().FileName;
            string fullPath = fileInfo?.FullPath ?? path;
            var reqBuilder = BuildItemRequestByPath(path);

            OneDriveFileHandle handle = new OneDriveFileHandle(CloudId, fileName, fullPath, length, itemInfo != null, isWrite: true, reqBuilder);

            return new CloudCachedFileStream(handle, Cache);
        }
    }
}
