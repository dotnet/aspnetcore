// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class HttpRequestStream : Stream
    {
        private readonly HttpRequestPipeReader _pipeReader;
        private readonly IHttpBodyControlFeature _bodyControl;
        private AsyncEnumerableReader _asyncReader;

        public HttpRequestStream(IHttpBodyControlFeature bodyControl, HttpRequestPipeReader pipeReader)
        {
            _bodyControl = bodyControl;
            _pipeReader = pipeReader;
        }

        public override bool CanSeek => false;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            try
            {
                return ReadAsyncInternal(destination, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousReadsDisallowed);
            }

            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

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

        private ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_asyncReader?.InProgress ?? false)
            {
                // Throw if there are overlapping reads; throwing unwrapped as it suggests last read was not awaited 
                // so we surface it directly rather than wrapped in a Task (as this one will likely also not be awaited).
                throw new InvalidOperationException("Concurrent reads are not supported; await the " + nameof(ValueTask<int>) + " before starting next read.");
            }

            try
            {
                while (true)
                {
                    if (!_pipeReader.TryRead(out var result))
                    {
                        break;
                    }

                    if (result.IsCanceled)
                    {
                        throw new OperationCanceledException("The read was canceled");
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

                            return new ValueTask<int>(actual);
                        }

                        if (result.IsCompleted)
                        {
                            return new ValueTask<int>(0);
                        }
                    }
                    finally
                    {
                        _pipeReader.AdvanceTo(consumed);
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValueTask<int>(Task.FromException<int>(ex));
            }

            var asyncReader = _asyncReader;
            if (asyncReader is null)
            {
                _asyncReader = asyncReader = new AsyncEnumerableReader();
                asyncReader.Initialize(ReadAsyncAwaited(asyncReader));
            }

            return asyncReader.ReadAsync(buffer, cancellationToken);
        }

        private async IAsyncEnumerable<int> ReadAsyncAwaited(AsyncEnumerableReader reader)
        {
            while (true)
            {
                var result = await _pipeReader.ReadAsync(reader.CancellationToken);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The read was canceled");
                }

                var readableBuffer = result.Buffer;
                var readableBufferLength = readableBuffer.Length;

                var consumed = readableBuffer.End;
                var advanced = false;
                try
                {
                    if (readableBufferLength != 0)
                    {
                        var actual = (int)Math.Min(readableBufferLength, reader.Buffer.Length);

                        var slice = actual == readableBufferLength ? readableBuffer : readableBuffer.Slice(0, actual);
                        consumed = slice.End;
                        slice.CopyTo(reader.Buffer.Span);

                        // Finally blocks in enumerators aren't excuted prior to the yield return,
                        // so we advance here
                        advanced = true;
                        _pipeReader.AdvanceTo(consumed);
                        yield return actual;
                    }
                    else if (result.IsCompleted)
                    {
                        // Finally blocks in enumerators aren't excuted prior to the yield return,
                        // so we advance here
                        advanced = true;
                        _pipeReader.AdvanceTo(consumed);
                        yield return 0;
                    }
                }
                finally
                {
                    if (!advanced)
                    {
                        _pipeReader.AdvanceTo(consumed);
                    }
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
                var result = await _pipeReader.ReadAsync(cancellationToken);
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
                    _pipeReader.AdvanceTo(readableBuffer.End);
                }
            }
        }
    }
}
