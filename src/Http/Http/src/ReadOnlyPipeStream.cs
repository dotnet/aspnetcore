// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Represents a read-only Stream backed by a PipeReader
    /// </summary>
    public class ReadOnlyPipeStream : Stream
    {
        private bool _allowSynchronousIO = true;

        /// <summary>
        /// Creates a new ReadOnlyPipeStream
        /// </summary>
        /// <param name="pipeReader">The PipeReader to read from.</param>
        public ReadOnlyPipeStream(PipeReader pipeReader) :
            this(pipeReader, allowSynchronousIO: true)
        {
        }

        /// <summary>
        /// Creates a new ReadOnlyPipeStream
        /// </summary>
        /// <param name="pipeReader">The PipeReader to read from.</param>
        /// <param name="allowSynchronousIO">Whether synchronous IO is allowed.</param>
        public ReadOnlyPipeStream(PipeReader pipeReader, bool allowSynchronousIO)
        {
            _allowSynchronousIO = allowSynchronousIO;
            InnerPipeReader = pipeReader;
        }

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public PipeReader InnerPipeReader { get; }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_allowSynchronousIO)
            {
                ThrowHelper.ThrowInvalidOperationException_SynchronousReadsDisallowed();
            }
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = ReadAsync(buffer, offset, count, default, state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).GetAwaiter().GetResult();
        }

        private Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = ReadAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith((task2, state2) =>
            {
                var tcs2 = (TaskCompletionSource<int>)state2;
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
                    tcs2.SetResult(task2.Result);
                }
            }, tcs, cancellationToken);
            return tcs.Task;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            return ReadAsyncInternal(destination, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await InnerPipeReader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                {
                    ThrowHelper.ThrowOperationCanceledException_ReadCanceled();
                }

                var readableBuffer = result.Buffer;
                var readableBufferLength = readableBuffer.Length;

                var consumed = readableBuffer.End;
                var actual = 0;
                try
                {
                    if (readableBufferLength != 0)
                    {
                        actual = (int)Math.Min(readableBufferLength, buffer.Length);

                        var slice = actual == readableBufferLength ? readableBuffer : readableBuffer.Slice(0, actual);
                        consumed = slice.End;
                        slice.CopyTo(buffer.Span);

                        return actual;
                    }

                    if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    InnerPipeReader.AdvanceTo(consumed);
                }
            }
        }

        /// <inheritdoc />
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            return CopyToAsyncInternal(destination, cancellationToken);
        }

        private async Task CopyToAsyncInternal(Stream destination, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await InnerPipeReader.ReadAsync(cancellationToken);
                var readableBuffer = result.Buffer;
                var readableBufferLength = readableBuffer.Length;

                try
                {
                    if (readableBufferLength != 0)
                    {
                        foreach (var memory in readableBuffer)
                        {
                            await destination.WriteAsync(memory, cancellationToken);
                        }
                    }

                    if (result.IsCompleted)
                    {
                        return;
                    }
                }
                finally
                {
                    InnerPipeReader.AdvanceTo(readableBuffer.End);
                }
            }
        }
    }
}
