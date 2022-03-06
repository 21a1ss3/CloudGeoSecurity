using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encryptor.Lib.OneDrive
{
    public class OneDriveFileHandle : ICloudFileHandle
    {
        public OneDriveFileHandle(Guid cloudId, string fileName, string fullPath, long length, bool exist, bool isWrite, IDriveItemRequestBuilder driveItemReqBuilder)
        {
            CloudId = cloudId;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Length = length;
            Exist = exist;
            IsWrite = isWrite;
            _driveItemReqBuilder = driveItemReqBuilder ?? throw new ArgumentNullException(nameof(driveItemReqBuilder));

            HandleId = Guid.NewGuid();
        }

        private IDriveItemRequestBuilder _driveItemReqBuilder;
        private string _cachedUrl;



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

            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            for (byte s = 0; s < 2; s++)
            {
                if (_cachedUrl == null)
                {
                    var driveItem = _driveItemReqBuilder.Request().GetAsync().GetAwaiter().GetResult();

                    if (!driveItem.AdditionalData.ContainsKey("@microsoft.graph.downloadUrl"))
                        throw new Exception("Unable to resolve download url for the file");

                    /*
                     * DO NOT Use casting to string, as shown in https://github.com/microsoftgraph/msgraph-sdk-dotnet/tree/dev/docs#downloadLargeFile
                     * The object type in the dictionary is "System.Text.Json.JsonElement", but not a plain string
                     * As shown in the tests it successfully returns a value by calling ToString() method
                    */
                    _cachedUrl = driveItem.AdditionalData["@microsoft.graph.downloadUrl"].ToString();
                }

                if (_cachedUrl != null)
                {
                    HttpRequestMessage reqMsg = new HttpRequestMessage(HttpMethod.Get, _cachedUrl);
                    reqMsg.Headers.Range = new RangeHeaderValue(offset, offset + size - 1);

                    HttpResponseMessage resMsg = _driveItemReqBuilder.Client.HttpProvider.SendAsync(reqMsg).GetAwaiter().GetResult();
                    
                    switch (resMsg.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.PartialContent:
                            break;
                        default:
                            _cachedUrl = null;
                            continue;
                    }

                    byte[] buffer = new byte[size];
                    int readed = 0;

                    using (Stream responseStream = resMsg.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    {
                        while (readed < size)
                            readed += responseStream.Read(buffer, readed, buffer.Length - readed);

                        responseStream.Close();
                    }

                    return buffer;
                }
            }

            throw new Exception("Unable to read file!");
        }

        public Task<Stream> SendFileAsync(long newLength)
        {
            if (newLength < 1)
                throw new ArgumentOutOfRangeException(nameof(newLength));
            
            TaskCompletionSource<Stream> taskCompletion = new TaskCompletionSource<Stream>();

            InternalUtils.StreamCatchHttpContent content = new InternalUtils.StreamCatchHttpContent(newLength);
            content.StreamCallBack += (stream) => taskCompletion.SetResult(stream);
            content.StreamClosed += (snd, eArg) => Length = newLength;

            var contReq = _driveItemReqBuilder.Content.Request();
            contReq.Method = HttpMethods.PUT;

            var httpReqMsg = contReq.GetHttpRequestMessage();
            httpReqMsg.Content = content;

            _driveItemReqBuilder.Client.HttpProvider.SendAsync(httpReqMsg).ConfigureAwait(false);

            return taskCompletion.Task;
        }      
    }
}
