// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class DuplexPipeStream : Stream
    {
        private readonly PipeReader _input;
        private readonly PipeWriter _output;
        private readonly bool _throwOnCancelled;
        private volatile bool _cancelCalled;

        private AsyncEnumerableReader _asyncReader;

        public DuplexPipeStream(PipeReader input, PipeWriter output, bool throwOnCancelled = false)
        {
            _input = input;
            _output = output;
            _throwOnCancelled = throwOnCancelled;
        }

        public void CancelPendingRead()
        {
            _cancelCalled = true;
            _input.CancelPendingRead();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // ValueTask uses .GetAwaiter().GetResult() if necessary
            // https://github.com/dotnet/corefx/blob/f9da3b4af08214764a51b2331f3595ffaf162abe/src/System.Threading.Tasks.Extensions/src/System/Threading/Tasks/ValueTask.cs#L156
            return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), default).Result;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            return ReadAsyncInternal(destination, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer != null)
            {
                _output.Write(new ReadOnlySpan<byte>(buffer, offset, count));
            }

            await _output.FlushAsync(cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            _output.Write(source.Span);
            await _output.FlushAsync(cancellationToken);
        }

        public override void Flush()
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return WriteAsync(null, 0, 0, cancellationToken);
        }

        private ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
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
                    if (!_input.TryRead(out var result))
                    {
                        break;
                    }

                    var readableBuffer = result.Buffer;
                    try
                    {
                        if (_throwOnCancelled && result.IsCanceled && _cancelCalled)
                        {
                            // Reset the bool
                            _cancelCalled = false;
                            throw new OperationCanceledException();
                        }

                        if (!readableBuffer.IsEmpty)
                        {
                            // buffer.Count is int
                            var count = (int)Math.Min(readableBuffer.Length, destination.Length);
                            readableBuffer = readableBuffer.Slice(0, count);
                            readableBuffer.CopyTo(destination.Span);
                            return new ValueTask<int>(count);
                        }

                        if (result.IsCompleted)
                        {
                            return new ValueTask<int>(0);
                        }
                    }
                    finally
                    {
                        _input.AdvanceTo(readableBuffer.End, readableBuffer.End);
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

            return asyncReader.ReadAsync(destination, cancellationToken);
        }

        private async IAsyncEnumerable<int> ReadAsyncAwaited(AsyncEnumerableReader reader)
        {
            while (true)
            {
                var result = await _input.ReadAsync(reader.CancellationToken);
                var readableBuffer = result.Buffer;

                var advanced = false;
                try
                {
                    if (_throwOnCancelled && result.IsCanceled && _cancelCalled)
                    {
                        // Reset the bool
                        _cancelCalled = false;
                        throw new OperationCanceledException();
                    }
                    else if (!readableBuffer.IsEmpty)
                    {
                        // buffer.Count is int
                        var count = (int)Math.Min(readableBuffer.Length, reader.Buffer.Length);
                        readableBuffer = readableBuffer.Slice(0, count);
                        readableBuffer.CopyTo(reader.Buffer.Span);

                        // Finally blocks in enumerators aren't excuted prior to the yield return,
                        // so we advance here
                        advanced = true;
                        _input.AdvanceTo(readableBuffer.End, readableBuffer.End);
                        yield return count;
                    }
                    else if (result.IsCompleted)
                    {
                        // Finally blocks in enumerators aren't excuted prior to the yield return,
                        // so we advance here
                        advanced = true;
                        _input.AdvanceTo(readableBuffer.End, readableBuffer.End);
                        yield return 0;
                    }
                }
                finally
                {
                    if (!advanced)
                    {
                        _input.AdvanceTo(readableBuffer.End, readableBuffer.End);
                    }
                }
            }
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

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = WriteAsync(buffer, offset, count, default, state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

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
    }
}
