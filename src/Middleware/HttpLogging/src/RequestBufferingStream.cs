// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class RequestBufferingStream : BufferingStream
    {
        private readonly Encoding _encoding;
        private readonly int _limit;

        public RequestBufferingStream(Stream innerStream, int limit, ILogger logger, Encoding encoding)
            : base(innerStream, logger)
        {
            _logger = logger;
            _limit = limit;
            _innerStream = innerStream;
            _encoding = encoding;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            var res = await _innerStream.ReadAsync(destination, cancellationToken);

            WriteToBuffer(destination.Slice(0, res).Span);

            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var res = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

            WriteToBuffer(buffer.AsSpan(offset, res));

            return res;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var res = _innerStream.Read(buffer, offset, count);

            WriteToBuffer(buffer.AsSpan(offset, res));

            return res;
        }

        private void WriteToBuffer(ReadOnlySpan<byte> span)
        {
            // get what was read into the buffer
            var remaining = _limit - _bytesBuffered;

            if (remaining == 0)
            {
                return;
            }

            if (span.Length == 0)
            {
                return;
            }

            var innerCount = Math.Min(remaining, span.Length);

            if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesBuffered += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }
    }
}
