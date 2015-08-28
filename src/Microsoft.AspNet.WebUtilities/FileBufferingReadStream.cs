// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.WebUtilities
{
    /// <summary>
    /// A Stream that wraps another stream and enables rewinding by buffering the content as it is read.
    /// The content is buffered in memory up to a certain size and then spooled to a temp file on disk.
    /// The temp file will be deleted on Dispose.
    /// </summary>
    public class FileBufferingReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _memoryThreshold;
        private string _tempFileDirectory;
        private readonly Func<string> _tempFileDirectoryAccessor;

        private Stream _buffer = new MemoryStream(); // TODO: We could have a more efficiently expanding buffer stream.
        private bool _inMemory = true;
        private bool _completelyBuffered;

        private bool _disposed;

        // TODO: allow for an optional buffer size limit to prevent filling hard disks. 1gb?
        public FileBufferingReadStream(
            [NotNull] Stream inner,
            int memoryThreshold,
            [NotNull] Func<string> tempFileDirectoryAccessor)
        {
            _inner = inner;
            _memoryThreshold = memoryThreshold;
            _tempFileDirectoryAccessor = tempFileDirectoryAccessor;
        }

        // TODO: allow for an optional buffer size limit to prevent filling hard disks. 1gb?
        public FileBufferingReadStream([NotNull] Stream inner, int memoryThreshold, [NotNull] string tempFileDirectory)
        {
            _inner = inner;
            _memoryThreshold = memoryThreshold;
            _tempFileDirectory = tempFileDirectory;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _buffer.Length; }
        }

        public override long Position
        {
            get { return _buffer.Position; }
            // Note this will not allow seeking forward beyond the end of the buffer.
            set
            {
                ThrowIfDisposed();
                _buffer.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            if (!_completelyBuffered && origin == SeekOrigin.End)
            {
                // Can't seek from the end until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            else if (!_completelyBuffered && origin == SeekOrigin.Current && offset + Position > Length)
            {
                // Can't seek past the end of the buffer until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            else if (!_completelyBuffered && origin == SeekOrigin.Begin && offset > Length)
            {
                // Can't seek past the end of the buffer until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            return _buffer.Seek(offset, origin);
        }

        private Stream CreateTempFile()
        {
            if (_tempFileDirectory == null)
            {
                Debug.Assert(_tempFileDirectoryAccessor != null);
                _tempFileDirectory = _tempFileDirectoryAccessor();
                Debug.Assert(_tempFileDirectory != null);
            }

            var fileName = Path.Combine(_tempFileDirectory, "ASPNET_" + Guid.NewGuid().ToString() + ".tmp");
            return new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 1024 * 16,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            if (_buffer.Position < _buffer.Length || _completelyBuffered)
            {
                // Just read from the buffer
                return _buffer.Read(buffer, offset, (int)Math.Min(count, _buffer.Length - _buffer.Position));
            }

            int read = _inner.Read(buffer, offset, count);

            if (_inMemory && _buffer.Length + read > _memoryThreshold)
            {
                var oldBuffer = _buffer;
                _buffer = CreateTempFile();
                _inMemory = false;
                oldBuffer.Position = 0;
                oldBuffer.CopyTo(_buffer, 1024 * 16);
            }

            if (read > 0)
            {
                _buffer.Write(buffer, offset, read);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }
#if DNX451
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            var tcs = new TaskCompletionSource<int>(state);
            BeginRead(buffer, offset, count, callback, tcs);
            return tcs.Task;
        }

        private async void BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, TaskCompletionSource<int> tcs)
        {
            try
            {
                var read = await ReadAsync(buffer, offset, count);
                tcs.TrySetResult(read);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (callback != null)
            {
                try
                {
                    callback(tcs.Task);
                }
                catch (Exception)
                {
                    // Suppress exceptions on background threads.
                }
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var task = (Task<int>)asyncResult;
            return task.GetAwaiter().GetResult();
        }
#endif
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_buffer.Position < _buffer.Length || _completelyBuffered)
            {
                // Just read from the buffer
                return await _buffer.ReadAsync(buffer, offset, (int)Math.Min(count, _buffer.Length - _buffer.Position), cancellationToken);
            }

            int read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);

            if (_inMemory && _buffer.Length + read > _memoryThreshold)
            {
                var oldBuffer = _buffer;
                _buffer = CreateTempFile();
                _inMemory = false;
                oldBuffer.Position = 0;
                await oldBuffer.CopyToAsync(_buffer, 1024 * 16, cancellationToken);
            }

            if (read > 0)
            {
                await _buffer.WriteAsync(buffer, offset, read, cancellationToken);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
#if DNX451
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }
#endif
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _buffer.Dispose();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileBufferingReadStream));
            }
        }
    }
}
