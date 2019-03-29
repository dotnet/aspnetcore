// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// A <see cref="Stream"/> that wraps another stream and buffers content to be written.
    /// <para>
    /// <see cref="FileBufferingWriteStream"/> is intended to provide a workaround for APIs that need to perform
    /// synchronous writes to the HTTP Response Stream while contending with a server that is configured to disallow synchronous writes.
    /// Synchronous writes are buffered to a memory backed stream up to the specified threshold, after which further writes are spooled to
    /// a temporary file on disk.
    /// </para>
    /// <para>
    /// The <see cref="FileBufferingWriteStream"/> performs opportunistic writes to the wrapping stream
    /// when asychronous operation such as <see cref="WriteAsync(byte[], int, int, CancellationToken)"/> or <see cref="FlushAsync(CancellationToken)"/>
    /// are performed.
    /// </para>
    /// </summary>
    public sealed class FileBufferingWriteStream : Stream
    {
        private const int MaxRentedBufferSize = 1024 * 1024; // 1MB
        private const int DefaultMemoryThreshold = 30 * 1024; // 30k

        private readonly Stream _writeStream;
        private readonly int _memoryThreshold;
        private readonly long? _bufferLimit;
        private readonly Func<string> _tempFileDirectoryAccessor;
        private readonly ArrayPool<byte> _bytePool;
        private readonly byte[] _rentedBuffer;

        /// <summary>
        /// Initializes a new instance of <see cref="FileBufferingWriteStream"/>.
        /// </summary>
        /// <param name="writeStream">The <see cref="Stream"/> to write buffered contents to.</param>
        /// <param name="memoryThreshold">
        /// The maximum amount of memory in bytes to allocate before switching to a file on disk.
        /// Defaults to 30kb.
        /// </param>
        /// <param name="bufferLimit">
        /// The maximum amouont of bytes that the <see cref="FileBufferingWriteStream"/> is allowed to buffer.
        /// </param>
        /// <param name="tempFileDirectoryAccessor">Provides the location of the directory to write buffered contents to.
        /// When unspecified, uses the value specified by the environment variable <c>ASPNETCORE_TEMP</c> if available, otherwise
        /// uses the value returned by <see cref="Path.GetTempPath"/>.
        /// </param>
        public FileBufferingWriteStream(
            Stream writeStream,
            int memoryThreshold = DefaultMemoryThreshold,
            long? bufferLimit = null,
            Func<string> tempFileDirectoryAccessor = null)
            : this(writeStream, memoryThreshold, bufferLimit, tempFileDirectoryAccessor, ArrayPool<byte>.Shared)
        {

        }

        internal FileBufferingWriteStream(
            Stream writeStream,
            int memoryThreshold,
            long? bufferLimit,
            Func<string> tempFileDirectoryAccessor,
            ArrayPool<byte> bytePool)
        {
            _writeStream = writeStream ?? throw new ArgumentNullException(nameof(writeStream));

            if (memoryThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(memoryThreshold));
            }

            if (bufferLimit != null && bufferLimit < memoryThreshold)
            {
                // We would expect a limit at least as much as memoryThreshold
                throw new ArgumentOutOfRangeException(nameof(bufferLimit), $"{nameof(bufferLimit)} must be larger than {nameof(memoryThreshold)}.");
            }

            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectoryAccessor = tempFileDirectoryAccessor ?? AspNetCoreTempDirectory.TempDirectoryFactory;
            _bytePool = bytePool;

            if (memoryThreshold < MaxRentedBufferSize)
            {
                _rentedBuffer = bytePool.Rent(memoryThreshold);
                MemoryStream = new MemoryStream(_rentedBuffer);
                MemoryStream.SetLength(0);
            }
            else
            {
                MemoryStream = new MemoryStream();
            }
        }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        internal long BufferedLength => MemoryStream.Length + (FileStream?.Length ?? 0);

        internal MemoryStream MemoryStream { get; }

        internal FileStream FileStream { get; private set; }

        internal bool Disposed { get; private set; }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowArgumentException(buffer, offset, count);
            ThrowIfDisposed();

            if (_bufferLimit.HasValue && _bufferLimit - BufferedLength < count)
            {
                DiposeInternal();
                throw new IOException("Buffer limit exceeded.");
            }

            var availableMemory = _memoryThreshold - MemoryStream.Position;
            if (count <= availableMemory)
            {
                // Buffer content in the MemoryStream if it has capacity.
                MemoryStream.Write(buffer, offset, count);
            }
            else
            {
                // If the MemoryStream is incapable of accomodating the content to be written
                // spool to disk.
                EnsureFileStream();
                CopyContent(MemoryStream, FileStream);
                FileStream.Write(buffer, offset, count);
            }
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentException(buffer, offset, count);
            ThrowIfDisposed();

            // If we have the opportunity to go async, write the buffered content to the response.
            await FlushAsync(cancellationToken);
            await _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        // In the ordinary case, we expect this to throw if the target is the HttpResponse Body
        // and disallows synchronous writes. We do not need to optimize for this.
        public override void Flush()
        {
            if (FileStream != null)
            {
                CopyContent(FileStream, _writeStream);
            }

            CopyContent(MemoryStream, _writeStream);
        }

        /// <inheritdoc />
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (FileStream != null)
            {
                await CopyContentAsync(FileStream, _writeStream, cancellationToken);
            }

            await CopyContentAsync(MemoryStream, _writeStream, cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                Disposed = true;
                Flush();

                DiposeInternal();
            }
        }

        private void DiposeInternal()
        {
            Disposed = true;
            _bytePool.Return(_rentedBuffer);
            MemoryStream.Dispose();
            FileStream?.Dispose();
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            if (!Disposed)
            {
                Disposed = true;
                try
                {
                    await FlushAsync();
                }
                finally
                {
                    if (_rentedBuffer != null)
                    {
                        _bytePool.Return(_rentedBuffer);
                    }
                    await MemoryStream.DisposeAsync();
                    await (FileStream?.DisposeAsync() ?? default);
                }
            }
        }

        private void EnsureFileStream()
        {
            if (FileStream == null)
            {
                var tempFileDirectory = _tempFileDirectoryAccessor();
                var tempFileName = Path.Combine(tempFileDirectory, "ASPNETCORE_" + Guid.NewGuid() + ".tmp");
                FileStream = new FileStream(
                    tempFileName,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.Delete,
                    bufferSize: 1,
                    FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FileBufferingReadStream));
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

        private static void CopyContent(Stream source, Stream destination)
        {
            source.Position = 0;
            source.CopyTo(destination);
            source.SetLength(0);
        }

        private static async Task CopyContentAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            source.Position = 0;
            await source.CopyToAsync(destination, cancellationToken);
            source.SetLength(0);
        }
    }
}
