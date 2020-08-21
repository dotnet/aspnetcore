// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class HttpResponseStream : Stream
    {
        private readonly HttpResponsePipeWriter _pipeWriter;
        private readonly IHttpBodyControlFeature _bodyControl;

        public HttpResponseStream(IHttpBodyControlFeature bodyControl, HttpResponsePipeWriter pipeWriter)
        {
            _bodyControl = bodyControl;
            _pipeWriter = pipeWriter;
        }

        public override bool CanSeek => false;

        public override bool CanRead => false;
        
        public override bool CanWrite => true;
        
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        
        public override int ReadTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
          => throw new NotSupportedException();
        
        public override void Flush()
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
            }

            FlushAsync(default).GetAwaiter().GetResult();
        }
        
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _pipeWriter.FlushAsync(cancellationToken).GetAsTask();
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
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
            }

            WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }
        
        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }
        
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return new ValueTask(WriteAsyncInternal(source, cancellationToken));
        }

        private Task WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return _pipeWriter.WriteAsync(source, cancellationToken).GetAsTask();
        }
    }
}
