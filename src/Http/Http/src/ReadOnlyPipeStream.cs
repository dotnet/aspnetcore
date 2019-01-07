// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class ReadOnlyPipeStream : WriteOnlyStream
    {
        private PipeWriter _pipeWriter;

        public ReadOnlyPipeStream(PipeWriter pipeWriter)
        {
            _pipeWriter = pipeWriter;
        }

        public override bool CanSeek => false;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            FlushAsync(default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _pipeWriter.FlushAsync(cancellationToken).AsTask();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = WriteAsync(buffer, offset, count, default(CancellationToken), state);
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

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            // For the first write, ensure headers are flushed if WriteDataAsync isn't called.
            //if (cancellationToken.IsCancellationRequested)
            //{
            //    return Task.FromCanceled(cancellationToken);
            //}

            //return _pipeWriter.WriteAsync(data, cancellationToken).AsTask();
        }
    }
}
