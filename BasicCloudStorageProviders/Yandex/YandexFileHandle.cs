using Encryptor.Lib.Yandex.HttpUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Encryptor.Lib.Yandex
{
    public class YandexFileHandle : ICloudFileHandle
    {
        public YandexFileHandle(Func<HttpMethod, Uri, HttpRequestMessage> requestBuilder, Guid cloudId, string fileName, string fullPath, long length, bool exist, bool isWrite, string tempPath)
        {
            _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
            CloudId = cloudId;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Length = length;
            Exist = exist;
            IsWrite = isWrite;

            _tempPath = tempPath;

            if (string.IsNullOrWhiteSpace(_tempPath))
                _tempPath = Environment.GetEnvironmentVariable("Temp");

            if (!Directory.Exists(_tempPath))
                throw new Exception("Temporary directory must exist");

            HandleId = Guid.NewGuid();
        }

        private Func<HttpMethod, Uri, HttpRequestMessage> _requestBuilder;
        private string _tempPath;


        public Guid CloudId { get; private set; }

        public string FileName { get; private set; }

        public string FullPath { get; private set; }

        public long Length { get; private set; }

        public bool Exist { get; private set; }

        public bool IsWrite { get; private set; }

        public Guid HandleId { get; }

        public byte[] FetchRange(long offset, int size)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = FullPath;

            using HttpRequestMessage reqMsg = _requestBuilder(HttpMethod.Get, pathBuilder.Uri);

            reqMsg.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, offset + (size - 1));

            using HttpClient httpClient = new HttpClient();
            using HttpResponseMessage httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult();

            if ((httpRespMsg.StatusCode != HttpStatusCode.OK) && (httpRespMsg.StatusCode != HttpStatusCode.PartialContent))
                throw new Exception($"Unable to process request, possibly path does not exist. Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");

            return httpRespMsg.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task<Stream> SendFileAsync(long newLength)
        {
            string tmpFilePath;
            Random rndGen = new Random();

            do
            {
                int fileNum = rndGen.Next(0x1000000, 0x1000000);

                tmpFilePath = Path.Combine(_tempPath, $"cloudSecurity_YDrive{fileNum:X7}.tmp");
            } while (File.Exists(tmpFilePath));

            YandexTempFileStream stream = new YandexTempFileStream(tmpFilePath);

            stream.StreamColsed += (snd, e) => _sendFileContinue(newLength, stream, tmpFilePath);

            return Task.FromResult<Stream>(stream);
        }

        private void _sendFileContinue(long newLength, YandexTempFileStream stream, string tempFilePath)
        {
            if (newLength != (new FileInfo(tempFilePath)).Length)
                throw new Exception("File sizes are not match!");

            byte[] md5Raw = stream.Md5Engine.Hash;
            byte[] sah256Raw = stream.Md5Engine.Hash;

            string md5hex = string.Join(string.Empty, md5Raw.Select(b => b.ToString("X2")));
            string sha256hex = string.Join(string.Empty, sah256Raw.Select(b => b.ToString("X2")));

            UriBuilder pathBuilder = new UriBuilder("https", "webdav.yandex.ru");
            pathBuilder.Path = FullPath;

            using HttpRequestMessage reqMsg = _requestBuilder(HttpMethod.Put, pathBuilder.Uri);
            reqMsg.Headers.Add("Etag", md5hex);
            reqMsg.Headers.Add("Sha256", sha256hex);

            try
            {
                using (Stream tmpFileStream = File.OpenRead(tempFilePath))
                {
                    using StreamContent content = new StreamContent(tmpFileStream);
                    reqMsg.Content = content;
                    reqMsg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/binary");
                    reqMsg.Headers.ExpectContinue = true;

                    using HttpClient httpClient = new HttpClient();
                    using HttpResponseMessage httpRespMsg = httpClient.SendAsync(reqMsg).ConfigureAwait(false).GetAwaiter().GetResult();

                    tmpFileStream.Close();
                    File.Delete(tempFilePath);

                    if (httpRespMsg.StatusCode == HttpStatusCode.Created)
                        return;

                    if (httpRespMsg.StatusCode == HttpStatusCode.InsufficientStorage)
                        throw new Exception("No enough space in the storage left");

                    if (httpRespMsg.StatusCode != HttpStatusCode.Continue)
                        throw new Exception($"Unable to process request! Details: {httpRespMsg.StatusCode:D} {httpRespMsg.ReasonPhrase}");
                }

            }
            finally
            {
                File.Delete(tempFilePath);
                
            }
        }
    }
}
