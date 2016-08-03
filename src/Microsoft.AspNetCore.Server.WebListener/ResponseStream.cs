// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="ResponseStreamAsyncResult.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.WebListener
{
    internal class ResponseStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Func<Task> _onStart;

        internal ResponseStream(Stream innerStream, Func<Task> onStart)
        {
            _innerStream = innerStream;
            _onStart = onStart;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override void SetLength(long value) => _innerStream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

#if !NETSTANDARD1_3
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _innerStream.EndRead(asyncResult);
        }
#endif
        public override void Flush()
        {
            _onStart().GetAwaiter().GetResult();
            _innerStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _onStart();
            await _innerStream.FlushAsync(cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _onStart().GetAwaiter().GetResult();
            _innerStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _onStart();
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
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

                if (callback != null)
                {
                    callback(tcs.Task);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
