using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encryptor.Lib
{
    public partial class InternalUtils
    {
        protected internal class CloseAwaitStream : Stream
        {
            public CloseAwaitStream(Stream baseStream)
            {
                BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            }

            public Stream BaseStream { get; private set; }

            public override bool CanRead => BaseStream.CanRead;

            public override bool CanSeek => BaseStream.CanSeek;

            public override bool CanWrite => BaseStream.CanWrite;

            public override long Length => BaseStream.Length;

            public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

            public override void Flush()
            {
                BaseStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return BaseStream.FlushAsync(cancellationToken);
            }



            public override int Read(byte[] buffer, int offset, int count)
            {
                return BaseStream.Read(buffer, offset, count);
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return BaseStream.BeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return BaseStream.EndRead(asyncResult);
            }

            public override int Read(Span<byte> buffer)
            {
                return BaseStream.Read(buffer);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return BaseStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return BaseStream.ReadAsync(buffer, cancellationToken);
            }

            public override int ReadByte()
            {
                return BaseStream.ReadByte();
            }

            public override int ReadTimeout { get => BaseStream.ReadTimeout; set => BaseStream.ReadTimeout = value; }





            public override long Seek(long offset, SeekOrigin origin)
            {
                return BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                BaseStream.SetLength(value);
            }




            public override void Write(byte[] buffer, int offset, int count)
            {
                BaseStream.Write(buffer, offset, count);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return BaseStream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                BaseStream.EndWrite(asyncResult);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                BaseStream.Write(buffer);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return BaseStream.WriteAsync(buffer, cancellationToken);
            }

            public override void WriteByte(byte value)
            {
                BaseStream.WriteByte(value);
            }

            public override int WriteTimeout { get => BaseStream.WriteTimeout; set => BaseStream.WriteTimeout = value; }




            public override bool CanTimeout => BaseStream.CanTimeout;

            public override void CopyTo(Stream destination, int bufferSize)
            {
                BaseStream.CopyTo(destination, bufferSize);
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return BaseStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                BaseStream.Dispose();
                base.Dispose(disposing);
            }

            public override ValueTask DisposeAsync()
            {
                base.DisposeAsync();

                return BaseStream.DisposeAsync();
            }

            public override bool Equals(object obj)
            {
                return BaseStream.Equals(obj) || base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return BaseStream.GetHashCode();
            }

            public override object InitializeLifetimeService()
            {
                return BaseStream.InitializeLifetimeService();
            }

            public override string ToString()
            {
                return BaseStream.ToString();
            }

            private bool _isOpen = true;

            public override void Close()
            {
                BaseStream.Close();
                base.Close();

                if (_isOpen)
                    Closed?.Invoke(this, EventArgs.Empty);

                _isOpen = false;
            }

            public event EventHandler Closed;
        }
    }
}
