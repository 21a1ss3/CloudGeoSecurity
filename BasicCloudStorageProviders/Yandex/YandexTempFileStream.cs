using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encryptor.Lib.Yandex
{
    public class YandexTempFileStream : FileStream
    {
        public YandexTempFileStream(string path) : base(path, FileMode.CreateNew)
        {
            Md5Engine = MD5.Create();
            Sha256Engine = SHA256.Create();
        }


        public MD5 Md5Engine { get; private set; }
        public SHA256 Sha256Engine { get; set; }

        public override void Write(byte[] array, int offset, int count)
        {
            base.Write(array, offset, count);

            Md5Engine.TransformBlock(array, offset, count, array, offset);
            Sha256Engine.TransformBlock(array, offset, count, array, offset);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            base.Write(buffer);

            byte[] array = buffer.ToArray();

            Md5Engine.TransformBlock(array, 0, array.Length, array, 0);
            Sha256Engine.TransformBlock(array, 0, array.Length, array, 0);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Task task =  base.WriteAsync(buffer, offset, count, cancellationToken);

            Md5Engine.TransformBlock(buffer, offset, count, buffer, offset);
            Sha256Engine.TransformBlock(buffer, offset, count, buffer, offset);

            return task;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ValueTask task = base.WriteAsync(buffer, cancellationToken);

            byte[] array = buffer.ToArray();

            Md5Engine.TransformBlock(array, 0, array.Length, array, 0);
            Sha256Engine.TransformBlock(array, 0, array.Length, array, 0);

            return task;
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);

            Md5Engine.TransformBlock(new byte[1] { value }, 0, 1, new byte[1], 0);
            Sha256Engine.TransformBlock(new byte[1] { value }, 0, 1, new byte[1], 0);
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback callback, object state)
        {
            IAsyncResult result = base.BeginWrite(array, offset, numBytes, callback, state);

            Md5Engine.TransformBlock(array, offset, numBytes, array, offset);
            Sha256Engine.TransformBlock(array, offset, numBytes, array, offset);

            return result;
        }


        public override void Close()
        {
            base.Close();

            Md5Engine.TransformFinalBlock(new byte[0], 0, 0);
            Sha256Engine.TransformFinalBlock(new byte[0], 0, 0);

            StreamColsed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler StreamColsed;
    }
}
