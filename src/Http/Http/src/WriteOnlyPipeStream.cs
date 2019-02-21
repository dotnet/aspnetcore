// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Represents a WriteOnlyStream backed by a PipeWriter
    /// </summary>
    public class WriteOnlyPipeStream : Stream
    {
        private bool _allowSynchronousIO = true;

        /// <summary>
        /// Creates a new WriteOnlyStream
        /// </summary>
        /// <param name="pipeWriter">The PipeWriter to write to.</param>
        public WriteOnlyPipeStream(PipeWriter pipeWriter) :
            this(pipeWriter, allowSynchronousIO: true)
        {
        }

        /// <summary>
        /// Creates a new WriteOnlyStream
        /// </summary>
        /// <param name="pipeWriter">The PipeWriter to write to.</param>
        /// <param name="allowSynchronousIO">Whether synchronous IO is allowed.</param>
        public WriteOnlyPipeStream(PipeWriter pipeWriter, bool allowSynchronousIO)
        {
            InnerPipeWriter = pipeWriter;
            _allowSynchronousIO = allowSynchronousIO;
        }

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanRead => false;

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

        /// <inheritdoc />
        public override int ReadTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public PipeWriter InnerPipeWriter { get; }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
          => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Flush()
        {
            if (!_allowSynchronousIO)
            {
                ThrowHelper.ThrowInvalidOperationException_SynchronousFlushesDisallowed();
            }

            FlushAsync(default).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return InnerPipeWriter.FlushAsync(cancellationToken).GetAsTask();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_allowSynchronousIO)
            {
                ThrowHelper.ThrowInvalidOperationException_SynchronousWritesDisallowed();
            }
            WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = WriteAsync(buffer, offset, count, default, state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((Task<object>)asyncResult).GetAwaiter().GetResult();
        }

        private Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            var task = WriteAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith((task2, state2) =>
            {
                var tcs2 = (TaskCompletionSource<object>)state2;
                if (task2.IsCanceled)
                {
                    tcs2.SetCanceled();
                }
                else if (task2.IsFaulted)
                {
                    tcs2.SetException(task2.Exception);
                }
                else
                {
                    tcs2.SetResult(null);
                }
            }, tcs, cancellationToken);
            return tcs.Task;
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return new ValueTask(WriteAsyncInternal(source, cancellationToken));
        }

        private Task WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return InnerPipeWriter.WriteAsync(source, cancellationToken).GetAsTask();
        }
    }
}
