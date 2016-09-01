// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal class ResponseCacheStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxBufferSize;

        public ResponseCacheStream(Stream innerStream, long maxBufferSize)
        {
            _innerStream = innerStream;
            _maxBufferSize = maxBufferSize;
        }

        public MemoryStream BufferedStream { get; } = new MemoryStream();

        public bool BufferingEnabled { get; set; } = true;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public void DisableBuffering()
        {
            BufferingEnabled = false;
            BufferedStream.SetLength(0);
            BufferedStream.Capacity = 0;
            BufferedStream.Dispose();
        }

        public override void SetLength(long value)
        {
            DisableBuffering();
            _innerStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            DisableBuffering();
            return _innerStream.Seek(offset, origin);
        }

        public override void Flush()
            => _innerStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _innerStream.FlushAsync();

        // Underlying stream is write-only, no need to override other read related methods
        public override int Read(byte[] buffer, int offset, int count)
            => _innerStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                _innerStream.Write(buffer, offset, count);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (BufferedStream.Length + count > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    BufferedStream.Write(buffer, offset, count);
                }
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (BufferedStream.Length + count > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    await BufferedStream.WriteAsync(buffer, offset, count, cancellationToken);
                }
            }
        }

        public override void WriteByte(byte value)
        {
            try
            {
                _innerStream.WriteByte(value);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (BufferedStream.Length + 1 > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    BufferedStream.WriteByte(value);
                }
            }
        }

#if NETSTANDARD1_3
        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
#else
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
#endif
        {
            return ToIAsyncResult(WriteAsync(buffer, offset, count), callback, state);
        }
#if NETSTANDARD1_3
        public void EndWrite(IAsyncResult asyncResult)
#else
        public override void EndWrite(IAsyncResult asyncResult)
#endif
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            ((Task)asyncResult).GetAwaiter().GetResult();
        }

        private static IAsyncResult ToIAsyncResult(Task task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(0);
                }

                callback?.Invoke(tcs.Task);
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
