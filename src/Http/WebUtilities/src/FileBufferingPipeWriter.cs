// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.WebUtilities
{
    // TODO: Implement spooling to disk
    // For perf it would be nice to write straight to the pipe until a threshold, to optimize performance for small payloads.
    // Later switch to a file stream approach, probably keeping a buffer to write in large chunks to disk.
    // This approach would be faster for small write but larger memory usage for big ones, so next will require to find new thresholds.
    // However it would probbaly not meet the buffering requirement.
    /// <summary>
    /// A <see cref="Stream"/> that buffers content to be written to disk.
    /// </summary>
    public sealed class FileBufferingPipeWriter : PipeWriter, IAsyncDisposable
    {
        private const int DefaultMemoryThreshold = 32 * 1024; // 32k
        private readonly PipeWriter _pipeWriter;
        private readonly int _memoryThreshold;
        private readonly long? _bufferLimit;
        private readonly Func<string> _tempFileDirectoryAccessor;

        private long _pipeWrittenBytes = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="FileBufferingWriteStream"/>.
        /// </summary>
        /// <param name="pipeWriter"></param>
        /// <param name="memoryThreshold">
        /// The maximum amount of memory in bytes to allocate before switching to a file on disk.
        /// Defaults to 32kb.
        /// </param>
        /// <param name="bufferLimit">
        /// The maximum amount of bytes that the <see cref="FileBufferingWriteStream"/> is allowed to buffer.
        /// </param>
        /// <param name="tempFileDirectoryAccessor">Provides the location of the directory to write buffered contents to.
        /// When unspecified, uses the value specified by the environment variable <c>ASPNETCORE_TEMP</c> if available, otherwise
        /// uses the value returned by <see cref="Path.GetTempPath"/>.
        /// </param>
        public FileBufferingPipeWriter(
            PipeWriter pipeWriter,
            int memoryThreshold = DefaultMemoryThreshold,
            long? bufferLimit = null,
            Func<string>? tempFileDirectoryAccessor = null)
        {
            if (memoryThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(memoryThreshold));
            }

            if (bufferLimit != null && bufferLimit < memoryThreshold)
            {
                // We would expect a limit at least as much as memoryThreshold
                throw new ArgumentOutOfRangeException(nameof(bufferLimit), $"{nameof(bufferLimit)} must be larger than {nameof(memoryThreshold)}.");
            }

            _pipeWriter = pipeWriter;
            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectoryAccessor = tempFileDirectoryAccessor ?? AspNetCoreTempDirectory.TempDirectoryFactory;
            PagedByteBuffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        }

        internal PagedByteBuffer PagedByteBuffer { get; }

        internal FileStream? FileStream { get; private set; }

        internal bool Disposed { get; private set; }

        private void EnsureFileStream()
        {
            if (FileStream == null)
            {
                var tempFileDirectory = _tempFileDirectoryAccessor();
                var tempFileName = Path.Combine(tempFileDirectory, "ASPNETCORE_" + Guid.NewGuid() + ".tmp");
                FileStream = new FileStream(
                    tempFileName,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Delete | FileShare.ReadWrite,
                    bufferSize: 1,
                    FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FileBufferingWriteStream));
            }
        }

        private static void ThrowArgumentException(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public override void Advance(int bytes)
        {
            _pipeWrittenBytes += bytes;
            _pipeWriter.Advance(bytes);
        }

        public override void CancelPendingFlush()
        {
            _pipeWriter.CancelPendingFlush();
        }

        public override void Complete(Exception? exception = null)
        {
            _pipeWriter.Complete(exception);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_pipeWrittenBytes + sizeHint > _memoryThreshold) throw new NotImplementedException();

            return _pipeWriter.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_pipeWrittenBytes + sizeHint > _memoryThreshold) throw new NotImplementedException();

            return _pipeWriter.GetSpan(sizeHint);
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            _pipeWrittenBytes = 0;
            return _pipeWriter.FlushAsync(cancellationToken);
        }

        public Task DrainBufferAsync()
        {
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            // TODO: perf
            await FlushAsync();
            Disposed = true;
        }
    }
}
