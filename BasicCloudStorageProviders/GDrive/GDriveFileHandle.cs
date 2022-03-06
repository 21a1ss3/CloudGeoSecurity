using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;
using GDriveData = Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;

namespace Encryptor.Lib.GDrive
{
    public class GDriveFileHandle : ICloudFileHandle
    {
        public GDriveFileHandle(DriveService apiClient, string fileId, string fileName, string parentId, string fullPath, long length, bool isWrite, Guid cloudId)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(nameof(fileName));

            if (string.IsNullOrWhiteSpace(parentId))
                throw new ArgumentNullException(nameof(parentId));

            CloudId = cloudId;
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            FileName = fileName;
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Length = length;
            IsWrite = isWrite;

            FileId = fileId;
            Exist = !string.IsNullOrWhiteSpace(FileId);

            ParentId = parentId;

            HandleId = Guid.NewGuid();
        }

        private DriveService _apiClient;

        public string ParentId { get; set; }

        public string FileId { get; private set; }

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

            if (!Exist || string.IsNullOrWhiteSpace(FileId))
                throw new Exception("Operation is not supported");

            using (IO.MemoryStream stream = new IO.MemoryStream(size))
            {
                var result = _apiClient.Files.Get(FileId).DownloadRange(stream, new System.Net.Http.Headers.RangeHeaderValue(offset, offset + size - 1));
                if (result.Status != Google.Apis.Download.DownloadStatus.Completed)
                    throw new Exception("Unable to download file!");

                return stream.ToArray();
            }
        }

        public Task<IO.Stream> SendFileAsync(long newLength)
        {
            if (newLength < 1)
                throw new ArgumentOutOfRangeException(nameof(newLength));

            InternalUtils.PipeQueueStream stream = new InternalUtils.PipeQueueStream();
            GDriveData.File file = new GDriveData.File();

            file.Parents = new string[] { ParentId };
            file.Name = FileName;
            if (!string.IsNullOrWhiteSpace(FileId))
                file.Id = FileId;

            var mediaUpload = _apiClient.Files.Create(file, stream, "application/octet-stream");
            IUploadProgress progress = null;

            stream.OnFlush += (snd, evArg) =>
                {
                    if (progress != null)
                        progress = mediaUpload.Resume();
                    else
                        progress = mediaUpload.Upload();

                    if (progress.Status == UploadStatus.Failed)
                        throw new Exception("Faield to upload file", progress.Exception);
                };

            
            return Task.FromResult<IO.Stream>(stream);
        }
    }
}
