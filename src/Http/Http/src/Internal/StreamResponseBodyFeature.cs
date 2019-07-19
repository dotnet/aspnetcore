// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    public class StreamResponseBodyFeature : IHttpResponseBodyFeature
    {
        private PipeWriter _pipeWriter;
        private bool _started;
        private bool _disposed;

        public StreamResponseBodyFeature(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public StreamResponseBodyFeature(Stream stream, IHttpResponseBodyFeature priorFeature)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            PriorFeature = priorFeature;
        }

        public Stream Stream { get; }

        public IHttpResponseBodyFeature PriorFeature { get; }

        public PipeWriter Writer
        {
            get
            {
                if (_pipeWriter == null)
                {
                    _pipeWriter = PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _pipeWriter;
            }
        }

        public virtual void DisableResponseBuffering()
        {
        }

        public virtual async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            if (!_started)
            {
                await StartAsync(cancellation);
            }
            await SendFileAsyncCore(Stream, path, offset, count, cancellation);
        }

        public virtual Task StartAsync(CancellationToken token = default)
        {
            if (!_started)
            {
                _started = true;
                return Stream.FlushAsync(token);
            }
            return Task.CompletedTask;
        }

        public virtual async Task CompleteAsync()
        {
            // CompleteAsync is registered with HttpResponse.OnCompleted and there's no way to unregister it.
            // Prevent it from running by marking as disposed.
            if (_disposed)
            {
                return;
            }

            if (!_started)
            {
                await StartAsync();
            }

            if (_pipeWriter != null)
            {
                await _pipeWriter.CompleteAsync();
            }
            else
            {
                await Stream.FlushAsync();
            }
        }

        // Not safe for overlapped writes.
        private static async Task SendFileAsyncCore(Stream outputStream, string fileName, long offset, long? count, CancellationToken cancel = default)
        {
            cancel.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(fileName);
            CheckRange(offset, count, fileInfo.Length);

            int bufferSize = 1024 * 16;
            var fileStream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: bufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                if (offset > 0)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                }

                await StreamCopyOperation.CopyToAsync(fileStream, outputStream, count, cancel);
            }
        }

        private static void CheckRange(long offset, long? count, long fileLength)
        {
            if (offset < 0 || offset > fileLength)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }
            if (count.HasValue &&
                (count.GetValueOrDefault() < 0 || count.GetValueOrDefault() > fileLength - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
