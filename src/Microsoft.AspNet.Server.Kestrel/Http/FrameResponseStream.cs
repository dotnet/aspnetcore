// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    class FrameResponseStream : Stream
    {
        private readonly FrameContext _context;

        public FrameResponseStream(FrameContext context)
        {
            _context = context;
        }

        public override void Flush()
        {
            //_write(default(ArraySegment<byte>), null);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();
            _context.FrameControl.Write(
                new ArraySegment<byte>(new byte[0]),
                (error, arg) =>
                {
                    var tcsArg = (TaskCompletionSource<int>)arg;
                    if (error != null)
                    {
                        tcsArg.SetException(error);
                    }
                    else
                    {
                        tcsArg.SetResult(0);
                    }
                },
                tcs);
            return tcs.Task;
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
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).Wait();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();
            _context.FrameControl.Write(
                new ArraySegment<byte>(buffer, offset, count),
                (error, arg) =>
                {
                    var tcsArg = (TaskCompletionSource<int>)arg;
                    if (error != null)
                    {
                        tcsArg.SetException(error);
                    }
                    else
                    {
                        tcsArg.SetResult(0);
                    }
                },
                tcs);
            return tcs.Task;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

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