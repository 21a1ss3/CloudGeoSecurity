using Microsoft.Extensions.Configuration;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Encryptor.Lib.Yandex
{
    public class YandexDriveCloud : CloudStorageBase
    {
        public YandexDriveCloud()
           : this(0)
        {

        }

        public YandexDriveCloud(int configIndex)
        {
            if (configIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            var appConfig = new ConfigurationBuilder()
                            .AddUserSecrets<YandexDriveCloud>()
                            .Build();

            List<YandexDriveConfiguration> configs = appConfig.GetSection("Clouds:YandexDrive")
                                                    .Get<List<YandexDriveConfiguration>>();

            if (configs == null)
                throw new Exception("No configuration has been found for Yandex Drive apps");

            if (configs.Count <= configIndex)
                throw new ArgumentOutOfRangeException(nameof(configIndex));

            _config = configs[configIndex];

            if (string.IsNullOrWhiteSpace(_config.ClientId))
                throw new ArgumentNullException("Yandex Drive client id must be configured!");
            if (string.IsNullOrWhiteSpace(_config.ClientSecret))
                throw new ArgumentNullException("Yandex Drive client secret must be configured!");
        }


        private static readonly string[] _scopes = { "login:avatar" , "yadisk:disk", "cloud_api:disk.app_folder",
                                                    "cloud_api:disk.read", "login:info", "cloud_api:disk.write",
                                                    "login:email",  "cloud_api:disk.info"};

        private YandexDriveConfiguration _config;
        private YandexOauthToken _token;
        


        public override string CloudProviderName => "Yandex cloud";

        public event EventHandler<YandexActionInfo> AuthentificationAction;



        public bool SignIn(YandexAuthOptions options)
        {
            string requestUri = "https://oauth.yandex.ru/authorize?response_type=code&";

            if (options == YandexAuthOptions.DeviceCode)
                requestUri = string.Empty;

            requestUri += $"client_id={WebUtility.UrlEncode(_config.ClientId)}";
            requestUri += $"&scope={WebUtility.UrlEncode(string.Join(' ', _scopes))}";
            YandexTokenResp tokenResp;
            string tokenRespStr;

            HttpClient httpClient;
            using (httpClient = new HttpClient())
            {
                switch (options)
                {
                    case YandexAuthOptions.DeviceCode:
                        HttpRequestMessage reqMsg;
                        HttpResponseMessage httpRespMsg;
                        string devCodesStr;

                        using (reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://oauth.yandex.ru/device/code"))
                        {
                            reqMsg.Content = new StringContent(requestUri);
                            reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                            using (httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult())
                            {
                                devCodesStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                                if (httpRespMsg.StatusCode != HttpStatusCode.OK)
                                    throw new Exception($"Request to Yandex OAuth has been failed with status: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase} and message: {devCodesStr}");

                            }
                        }

                        YandexDevCodeResp devCodes = JsonSerializer.Deserialize<YandexDevCodeResp>(devCodesStr);

                        if (!string.IsNullOrEmpty(devCodes.ErrorCode))
                            throw new Exception($"Request to Yandex OAuth has been failed with message: {devCodesStr}");


                        DateTime deadline = DateTime.Now.AddSeconds(devCodes.ExpiresIn);
                        AuthentificationAction?.Invoke(this, new YandexActionInfo(devCodes.UserCode, devCodes.VerificationUri));

                        do
                        {
                            Task.Delay(25 * 1000).Wait();

                            if (deadline <= DateTime.Now)
                                return false;
                            
                            using (reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://oauth.yandex.ru/token"))
                            {
                                requestUri = "grant_type=device_code" +
                                                $"&code={WebUtility.UrlEncode(devCodes.DeviceCode)}" +
                                                $"&client_id={WebUtility.UrlEncode(_config.ClientId)}" +
                                                $"&client_secret={WebUtility.UrlEncode(_config.ClientSecret)}";

                                reqMsg.Content = new StringContent(requestUri);
                                reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                                using (httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult())
                                {
                                    tokenRespStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                                    tokenResp = JsonSerializer.Deserialize<YandexTokenResp>(tokenRespStr);

                                }
                            }
                        } while (tokenResp.ErrorCode == "authorization_pending");


                        break;
                    case YandexAuthOptions.EmbeddedBrowser:
                        throw new NotImplementedException();
                    case YandexAuthOptions.CustomBrowser:
                        int portNumber;
                        var ipEndpoint = new IPAddress(new byte[] { 127, 0, 0, 6 });

                        TcpListener tcpTestListener = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, 6 }), 0);
                        try
                        {
                            tcpTestListener.Start();
                            portNumber = ((IPEndPoint)tcpTestListener.LocalEndpoint).Port;
                            tcpTestListener.Stop();
                        }
                        catch (Exception ex)
                        {
                            tcpTestListener.Stop();
                            throw new Exception("Unable to allocate a port to listen!", ex);
                        }
                        finally
                        {
                            tcpTestListener.Stop();
                        }

                        Uri redirectUri = new UriBuilder("http", ipEndpoint.ToString(), portNumber).Uri;

                        HttpListener httpListener = new HttpListener();
                        httpListener.Prefixes.Add(redirectUri.ToString());
                        httpListener.Start();

                        requestUri += $"&redirect_uri={WebUtility.UrlEncode(redirectUri.ToString())}";

                        AuthentificationAction?.Invoke(this, new YandexActionInfo(new Uri(requestUri)));


                        HttpListenerContext httpContext = httpListener.GetContext();
                        NameValueCollection queryStr = httpContext.Request.QueryString;

                        byte[] clientResponse = Encoding.UTF8.GetBytes("We successfully complete authentication. You can safely close this page.");

                        httpContext.Response.StatusCode = 200;
                        httpContext.Response.StatusDescription = "OK";
                        httpContext.Response.ContentType = "text/plain";
                        httpContext.Response.ContentLength64 = clientResponse.LongLength;
                        httpContext.Response.SendChunked = false;
                        httpContext.Response.KeepAlive = false;

                        using (httpContext.Response.OutputStream)
                        {
                            httpContext.Response.OutputStream.Write(clientResponse, 0, clientResponse.Length);
                            httpContext.Response.OutputStream.Flush();
                            httpContext.Response.OutputStream.Close();
                        }

                        httpContext.Response.Close();

                        string code = queryStr["code"];

                        using (reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://oauth.yandex.ru/token"))
                        {
                            requestUri = "grant_type=authorization_code" +
                                            $"&code={WebUtility.UrlEncode(code)}" +
                                            $"&client_id={WebUtility.UrlEncode(_config.ClientId)}" +
                                            $"&client_secret={WebUtility.UrlEncode(_config.ClientSecret)}";

                            reqMsg.Content = new StringContent(requestUri);
                            reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                            using (httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult())
                            {
                                tokenRespStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                                tokenResp = JsonSerializer.Deserialize<YandexTokenResp>(tokenRespStr);
                            }
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }

            }

            if (!string.IsNullOrWhiteSpace(tokenResp.ErrorCode))
                throw new Exception($"Unable to acquire token because of error. {tokenResp.ErrorCode} {tokenResp.ErrorDescription}");

            _token =  new YandexOauthToken(tokenResp.AccessToken, tokenResp.RefreshToken, tokenResp.TokenType, tokenResp.Scope, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(tokenResp.ExpiresIn));
            IsAuthenticated = true;

            return true;
        }

        protected (bool status, bool tokenRefreshed) RefreshToken()
        {
            if (_token == null)
                return (false, false);

            if (DateTime.UtcNow < _token.Expired.AddMinutes(1))
                return (true, false);

            using HttpClient httpClient = new HttpClient();
            using HttpRequestMessage reqMsg = new HttpRequestMessage(HttpMethod.Post, "https://oauth.yandex.ru/token");

            string requestUri = "grant_type=refresh_token" +
                                $"&refresh_token={WebUtility.UrlEncode(_token.RefreshToken)}" +
                                $"&client_id={WebUtility.UrlEncode(_config.ClientId)}" +
                                $"&client_secret={WebUtility.UrlEncode(_config.ClientSecret)}";

            reqMsg.Content = new StringContent(requestUri);
            reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            
            using HttpResponseMessage httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult();

            string tokenRespStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            YandexTokenResp tokenResp = JsonSerializer.Deserialize<YandexTokenResp>(tokenRespStr);

            if (tokenResp.ErrorCode != null)
            {
                IsAuthenticated = false;
                return (false, false);
            }

            _token = new YandexOauthToken(tokenResp.AccessToken, tokenResp.RefreshToken, tokenResp.TokenType, tokenResp.Scope, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(tokenResp.ExpiresIn));
            IsAuthenticated = true;

            return (true, true);
        }

        protected HttpRequestMessage ProduceAuthRequestMsg(HttpMethod method, Uri url)
        {
            if (!RefreshToken().status)
                throw new Exception("Yandex disk cloud shall be authenticted first!");

            HttpRequestMessage reqMsg = new HttpRequestMessage(method, url);
            reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", _token.AccessToken);
            reqMsg.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            return reqMsg;
        }

        protected HttpRequestMessage ProduceListRequest(Uri url)
        {
            HttpRequestMessage reqMsg = ProduceAuthRequestMsg(new HttpMethod("PROPFIND"), url);

            reqMsg.Headers.Add("Depth", "1");

            return reqMsg;
        }

        protected HttpRequestMessage ProduceCurrentRequest(Uri url)
        {
            HttpRequestMessage reqMsg = ProduceAuthRequestMsg(new HttpMethod("PROPFIND"), url);

            reqMsg.Headers.Add("Depth", "0");

            return reqMsg;
        }

        protected HttpResponseMessage SendMessage(HttpRequestMessage requestMessage)
        {
            using HttpClient httpClient = new HttpClient();
            return httpClient.SendAsync(requestMessage).ConfigureAwait(false).GetAwaiter().GetResult();
        }


        public override void CreateDirectory(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            using HttpRequestMessage reqMsg = ProduceAuthRequestMsg(new HttpMethod("MKCOL"), pathBuilder.Uri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.Created)
            {
                if (httpRespMsg.StatusCode == HttpStatusCode.Conflict)
                    throw new Exception("Parent directory does not exist!");

                throw new Exception($"Failed to create a directory. Got repsonse {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");
            }
        }

        public override ICloudItemInfo[] EnumItems(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            Uri resourceUri = pathBuilder.Uri;
            using HttpRequestMessage reqMsg = ProduceListRequest(resourceUri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.MultiStatus)
                throw new Exception($"Unable to process request, possibly path does not exist. Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");

            string respStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            List<HttpUtils.YandexSdkDriveItem> drvItems = HttpUtils.SdkResponseParser.ParseItems(resourceUri.ToString(), respStr);
            List<ICloudItemInfo> items = new List<ICloudItemInfo>();

            foreach (var drvItem in drvItems.Skip(1))
            {
                if (drvItem.IsDirectory)
                    items.Add(new YandexDirectoryInfo(drvItem));
                else
                    items.Add(new YandexDriveFileInfo(drvItem));
            }

            return items.ToArray();
        }

        public override ICloudItemInfo GetItemInfo(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            Uri resourceUri = pathBuilder.Uri;
            using HttpRequestMessage reqMsg = ProduceCurrentRequest(resourceUri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.MultiStatus)
                return null;

            string respStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            List<HttpUtils.YandexSdkDriveItem> drvItems = HttpUtils.SdkResponseParser.ParseItems(resourceUri.ToString(), respStr);
            
            if (drvItems.Count == 0)
                return null;

            if (drvItems[0].IsDirectory)
                return new YandexDirectoryInfo(drvItems[0]);

            return new YandexDriveFileInfo(drvItems[0]);
        }

        public override bool DirectoryExists(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            Uri resourceUri = pathBuilder.Uri;
            using HttpRequestMessage reqMsg = ProduceCurrentRequest(resourceUri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.MultiStatus)
                return false;

            string respStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            List<HttpUtils.YandexSdkDriveItem> drvItems = HttpUtils.SdkResponseParser.ParseItems(resourceUri.ToString(), respStr);

            if (drvItems.Count == 0)
                return false;

            return drvItems[0].IsDirectory;
        }


        public override bool FileExists(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            Uri resourceUri = pathBuilder.Uri;
            using HttpRequestMessage reqMsg = ProduceCurrentRequest(resourceUri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.MultiStatus)
                return false;

            string respStr = httpRespMsg.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            List<HttpUtils.YandexSdkDriveItem> drvItems = HttpUtils.SdkResponseParser.ParseItems(resourceUri.ToString(), respStr);

            if (drvItems.Count == 0)
                return false;

            return !drvItems[0].IsDirectory;
        }

        public override long GetFileSize(string path)
        {
            ICloudFileInfo fileInfo = GetItemInfo(path) as ICloudFileInfo;

            if (fileInfo == null)
                throw new Exception("File not found or path points towards a directory");

            return fileInfo.FileSize;
        }




        public override byte[] ReadAllBytes(string path)
        {
            ValidatePath(path);

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            using HttpRequestMessage reqMsg = ProduceAuthRequestMsg(HttpMethod.Get, pathBuilder.Uri);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Unable to process request, possibly file does not exist! Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");

            byte[] result = httpRespMsg.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            //TODO: Check hash

            return result;
        }

        public override byte[] ReadFirstBytes(string path, int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (size == 0)
                return new byte[0];

            ValidatePath(path);

            ICloudFileInfo fileInfo = GetItemInfo(path) as ICloudFileInfo;

            if (fileInfo == null)
                throw new Exception("File not found or path points towards directory or there was error in request");

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            using HttpRequestMessage reqMsg = ProduceAuthRequestMsg(HttpMethod.Get, pathBuilder.Uri);
            reqMsg.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, Math.Min(fileInfo.FileSize, size) - 1);
            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if ((httpRespMsg.StatusCode != HttpStatusCode.OK) && (httpRespMsg.StatusCode != HttpStatusCode.PartialContent))
                throw new Exception($"Unable to process request, possibly file does not exist! Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");

            return httpRespMsg.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override void WriteAllBytes(string path, byte[] content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            ValidatePath(path);

            var md5Eng = System.Security.Cryptography.MD5.Create();
            var sha256Eng = System.Security.Cryptography.SHA256.Create();

            byte[] md5Raw = md5Eng.ComputeHash(content);
            byte[] sah256Raw = sha256Eng.ComputeHash(content);

            string md5hex = string.Join(string.Empty, md5Raw.Select(b => b.ToString("X2")));
            string sha256hex = string.Join(string.Empty, sah256Raw.Select(b => b.ToString("X2")));

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = path;
            using HttpRequestMessage reqMsg = ProduceAuthRequestMsg(HttpMethod.Put, pathBuilder.Uri);
            reqMsg.Headers.Add("Etag", md5hex);
            reqMsg.Headers.Add("Sha256", sha256hex);

            reqMsg.Content = new ByteArrayContent(content);
            reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/binary");
            reqMsg.Headers.ExpectContinue = true;

            using HttpResponseMessage httpRespMsg = SendMessage(reqMsg);

            if (httpRespMsg.StatusCode == HttpStatusCode.Created)
                return; 

            if (httpRespMsg.StatusCode == HttpStatusCode.InsufficientStorage)
                throw new Exception("No enough space in the storage left");

            if (httpRespMsg.StatusCode != HttpStatusCode.Continue)
                throw new Exception($"Unable to process request! Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");
        }

        public override Stream FileOpenRead(string path)
        {
            ValidatePath(path);

            if (Cache == null)
                throw new Exception("You must configure cache first");

            ICloudFileInfo fileInfo = GetItemInfo(path) as ICloudFileInfo;

            if ((fileInfo == null))
                throw new Exception($"File with {nameof(path)} not found or it is pointing not to a file!");

            YandexFileHandle handle = new YandexFileHandle(
                                                              ProduceAuthRequestMsg,
                                                              CloudId,
                                                              fileInfo.Name,
                                                              fileInfo.FullPath,
                                                              fileInfo.FileSize,
                                                              exist: true,
                                                              isWrite: false,
                                                              ".\\" );

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

            YandexFileHandle handle = new YandexFileHandle(ProduceAuthRequestMsg, CloudId, fileName, fullPath, length, exist: itemInfo != null, isWrite: true, ".\\");

            return new CloudCachedFileStream(handle, Cache);
        }
    }
}
