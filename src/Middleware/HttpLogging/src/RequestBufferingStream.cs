// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class RequestBufferingStream : BufferingStream
    {
        private Stream _innerStream;
        private Encoding? _encoding;
        private readonly int _limit;
        private bool _hasLogged;

        public RequestBufferingStream(Stream innerStream, int limit, ILogger logger, Encoding? encoding)
            : base(logger)
        {
            _limit = limit;
            _innerStream = innerStream;
            _encoding = encoding;
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

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            var res = await _innerStream.ReadAsync(destination, cancellationToken);

            WriteToBuffer(destination.Slice(0, res).Span, res);

            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var res = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            WriteToBuffer(buffer.AsSpan(offset, res), res);

            return res;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var res = _innerStream.Read(buffer, offset, count);

            WriteToBuffer(buffer.AsSpan(offset, res), res);

            return res;
        }

        private void WriteToBuffer(ReadOnlySpan<byte> span, int res)
        {
            // get what was read into the buffer
            var remaining = _limit - _bytesWritten;

            if (remaining == 0)
            {
                return;
            }

            if (res == 0 && !_hasLogged)
            {
                // Done reading, log the string.
                LogString(_encoding, LoggerEventIds.RequestBody, CoreStrings.RequestBody);
                _hasLogged = true;
                return;
            }

            var innerCount = Math.Min(remaining, span.Length);

            if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesWritten += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
            }

            if (_limit - _bytesWritten == 0 && !_hasLogged)
            {
                LogString(_encoding, LoggerEventIds.RequestBody, CoreStrings.RequestBody);
                _hasLogged = true;
            }
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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        /// <inheritdoc />
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var remaining = _limit - _bytesWritten;

            while (remaining > 0)
            {
                // reusing inner buffer here between streams
                var memory = GetMemory();
                var innerCount = Math.Min(remaining, memory.Length);

                var res = await _innerStream.ReadAsync(memory.Slice(0, innerCount), cancellationToken);

                _tailBytesBuffered += res;
                _bytesWritten += res;
                _tailMemory = _tailMemory.Slice(res);

                await destination.WriteAsync(memory.Slice(0, res));

                remaining -= res;
            }

            await _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }
}
