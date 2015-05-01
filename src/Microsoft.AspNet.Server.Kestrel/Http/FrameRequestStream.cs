// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class FrameRequestStream : Stream
    {
        readonly MessageBody _body;

        //int _readLength;
        //bool _readFin;
        //Exception _readError;

        public FrameRequestStream(MessageBody body)
        {
            _body = body;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

#if DNX451
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = ReadAsync(buffer, offset, count, CancellationToken.None, state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).Result;
        }
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _body.ReadAsync(new ArraySegment<byte>(buffer, offset, count));
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = _body.ReadAsync(new ArraySegment<byte>(buffer, offset, count));
            task.ContinueWith((t, x) =>
            {
                var tcs2 = (TaskCompletionSource<int>)x;
                if (t.IsCanceled)
                {
                    tcs2.SetCanceled();
                }
                else if (t.IsFaulted)
                {
                    tcs2.SetException(t.Exception);
                }
                else
                {
                    tcs2.SetResult(t.Result);
                }
            }, tcs);
            return tcs.Task;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position { get; set; }
    }
}
