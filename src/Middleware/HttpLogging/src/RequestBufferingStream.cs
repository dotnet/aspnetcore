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
    internal sealed class RequestBufferingStream : BufferingStream
    {
        private Encoding _encoding;
        private readonly int _limit;

        public bool HasLogged { get; private set; }

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
            var remaining = _limit - _bytesBuffered;

            if (remaining == 0)
            {
                return;
            }

            if (res == 0 && !HasLogged)
            {
                // Done reading, log the string.
                LogRequestBody();
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

            if (_limit - _bytesBuffered == 0 && !HasLogged)
            {
                var requestBody = GetString(_encoding);
                if (requestBody != null)
                {
                    _logger.RequestBody(requestBody);
                }
                HasLogged = true;
            }
        }

        public void LogRequestBody()
        {
            var requestBody = GetString(_encoding);
            if (requestBody != null)
            {
                _logger.RequestBody(requestBody);
            }
            HasLogged = true;
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
