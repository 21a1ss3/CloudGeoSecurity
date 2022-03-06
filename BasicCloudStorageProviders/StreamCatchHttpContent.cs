using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Encryptor.Lib
{
    public partial class InternalUtils
    {
        protected internal class StreamCatchHttpContent : HttpContent
        {
            public StreamCatchHttpContent(long length)
            {
                Length = length;
                Headers.ContentType = _mimeType;
            }

            private readonly static MediaTypeHeaderValue _mimeType = new MediaTypeHeaderValue("application/octet-stream");

            public long Length { get; set; }

            public event Action<Stream> StreamCallBack;
            public event EventHandler StreamClosed;

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                CloseAwaitStream internalStream = new CloseAwaitStream(stream);
                TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();

                internalStream.Closed += (obj, evArgs) =>
                {
                    StreamClosed?.Invoke(this, EventArgs.Empty);
                    taskCompletion.SetResult(true);
                };

                StreamCallBack?.Invoke(internalStream);

                return taskCompletion.Task;
            }

            protected override bool TryComputeLength(out long length)
            {
                length = Length;

                return true;
            }
        }
    }
}
