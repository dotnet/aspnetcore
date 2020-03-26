// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.TestHost
{
    internal class ResponseBodyWriterStream : Stream
    {
        private readonly ResponseBodyPipeWriter _responseWriter;
        private readonly Func<bool> _allowSynchronousIO;

        public ResponseBodyWriterStream(ResponseBodyPipeWriter responseWriter, Func<bool> allowSynchronousIO)
        {
            _responseWriter = responseWriter;
            _allowSynchronousIO = allowSynchronousIO;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

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
            FlushAsync().GetAwaiter().GetResult();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _responseWriter.FlushAsync(cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_allowSynchronousIO())
            {
                throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true.");
            }

            // The Pipe Write method requires calling FlushAsync to notify the reader. Call WriteAsync instead.
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _responseWriter.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }
    }
}
