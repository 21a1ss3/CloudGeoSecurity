using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Encryptor.Lib
{
    public partial class InternalUtils
    {
        protected internal class PipeQueueStream : Stream
        {

            protected LinkedList<QueueItem> Queue = new LinkedList<QueueItem>();
            protected LinkedList<QueueItem> ReuseQueueItems = new LinkedList<QueueItem>();
            protected QueueItem Current = null;
            private long _position = 0;


            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException("The stream does not support seeking.");

            public override long Position { get => _position; set => throw new NotSupportedException("The stream does not support seeking."); }



            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException("The stream does not support seeking, such as if the stream is constructed from a pipe or console output.");
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output.");
            }

            protected QueueItem ProduceNewQueueItem()
            {
                if (ReuseQueueItems.Count == 0)
                    return new QueueItem();

                QueueItem queueItem = ReuseQueueItems.First.Value;
                ReuseQueueItems.RemoveFirst();

                queueItem.Buffer = null;
                queueItem.Readed = 0;

                return queueItem;
            }

            protected void UtiliseQueueItem(QueueItem item)
            {
                item.Buffer = null;
                ReuseQueueItems.AddLast(item);
            }

            protected virtual void FetchActualReadItem()
            {
                if ((Current == null) || (Current.Readed == Current.Buffer.Length))
                {
                    if (Current != null)
                        UtiliseQueueItem(Current);

                    if (Queue.Count == 0)
                        Current = null;

                    Current = Queue.First.Value;
                    Queue.RemoveFirst();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if ((offset < 0) || (offset >= buffer.Length))
                    throw new ArgumentOutOfRangeException(nameof(offset));

                if ((count < 0) || (buffer.Length < (offset + count)))
                    throw new ArgumentOutOfRangeException(nameof(count));
                
                if (count == 0)
                    return 0;

                FetchActualReadItem();

                if (Current == null)
                    return 0;

                int readSize = Math.Min(count, Current.Buffer.Length - Current.Readed);

                Array.Copy(Current.Buffer, Current.Readed, buffer, offset, count);

                Current.Readed += readSize;

                return readSize;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if ((offset < 0) || (offset >= buffer.Length))
                    throw new ArgumentOutOfRangeException(nameof(offset));

                if ((count < 0) || (buffer.Length < (offset + count)))
                    throw new ArgumentOutOfRangeException(nameof(count));
                
                if (count == 0)
                    return;

                QueueItem item = ProduceNewQueueItem();

                item.Buffer = new byte[count];

                Array.Copy(buffer, offset, item.Buffer, 0, count);

                Queue.AddLast(item);
            }

            public override void Flush()
            {
                OnFlush?.Invoke(this, EventArgs.Empty);
            }

            public event EventHandler OnFlush;

            protected class QueueItem
            {
                public byte[] Buffer { get; set; }
                public int Readed { get; set; }
            }
        }
        
    }
}
