// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class HttpRequestStream : Stream
    {
        private readonly HttpRequestPipeReader _pipeReader;
        private readonly IHttpBodyControlFeature _bodyControl;

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
            return ReadAsyncWrapper(destination, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsyncWrapper(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
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
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);   
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        private ValueTask<int> ReadAsyncWrapper(Memory<byte> destination, CancellationToken cancellationToken)
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

        private async ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await _pipeReader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The read was canceled");
                }

                var buffer = result.Buffer;
                var length = buffer.Length;

                var consumed = buffer.End;
                try
                {
                    if (length != 0)
                    {
                        var actual = (int)Math.Min(length, destination.Length);

                        var slice = actual == length ? buffer : buffer.Slice(0, actual);
                        consumed = slice.End;
                        slice.CopyTo(destination.Span);

                        return actual;
                    }

                    if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _pipeReader.AdvanceTo(consumed);
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

            return _pipeReader.CopyToAsync(destination, cancellationToken);
        }
    }
}
