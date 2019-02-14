// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    // Think this is close to good
    public class ForContentLength : Http1MessageBody
    {
        private readonly long _contentLength;
        private long _inputLength;
        private ReadResult _previousReadResult; // TODO we can probably make this in Http1MessageBody or even MessageBody

        public ForContentLength(bool keepAlive, long contentLength, Http1Connection context)
            : base(context)
        {
            RequestKeepAlive = keepAlive;
            _contentLength = contentLength;
            _inputLength = _contentLength;
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_inputLength == 0)
            {
                throw new InvalidOperationException("Attempted to read from completed Content-Length request body.");
            }

            TryStart();

            while (true)
            {
                _previousReadResult = await StartTimingReadAsync(cancellationToken);
                var readableBuffer = _previousReadResult.Buffer;
                var readableBufferLength = readableBuffer.Length;
                StopTimingRead(readableBufferLength);

                if (readableBufferLength != 0)
                {
                    break;
                }

                if (_previousReadResult.IsCompleted)
                {
                    TryStop();
                    break;
                }
            }

            // handle cases where we send more data than the content length
            if (_previousReadResult.Buffer.Length > _inputLength)
            {
                _previousReadResult = new ReadResult(_previousReadResult.Buffer.Slice(0, _inputLength), _previousReadResult.IsCanceled, isCompleted: true);

            }
            else if (_previousReadResult.Buffer.Length == _inputLength)
            {
                _previousReadResult = new ReadResult(_previousReadResult.Buffer, _previousReadResult.IsCanceled, isCompleted: true);
            }

            return _previousReadResult;
        }

        public override bool TryRead(out ReadResult readResult)
        {
            var res = _context.Input.TryRead(out _previousReadResult);

            if (_previousReadResult.Buffer.Length > _inputLength)
            {
                _previousReadResult = new ReadResult(_previousReadResult.Buffer.Slice(0, _inputLength), _previousReadResult.IsCanceled, isCompleted: true);

            }
            else if (_previousReadResult.Buffer.Length == _inputLength)
            {
                _previousReadResult = new ReadResult(_previousReadResult.Buffer, _previousReadResult.IsCanceled, isCompleted: true);
            }

            readResult = _previousReadResult;
            return res;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            var dataLength = _previousReadResult.Buffer.Slice(_previousReadResult.Buffer.Start, consumed).Length;
            _inputLength -= dataLength;
            _context.Input.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            var dataLength = _previousReadResult.Buffer.Slice(_previousReadResult.Buffer.Start, consumed).Length;
            _inputLength -= dataLength;
            _context.Input.AdvanceTo(consumed, examined);
        }

        protected override void OnReadStarting()
        {
            if (_contentLength > _context.MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }
        }

        private ValueTask<ReadResult> StartTimingReadAsync(CancellationToken cancellationToken)
        {
            var readAwaitable = _context.Input.ReadAsync(cancellationToken);

            if (!readAwaitable.IsCompleted && _timingEnabled)
            {
                _backpressure = true;
                _context.TimeoutControl.StartTimingRead();
            }

            return readAwaitable;
        }

        private void StopTimingRead(long bytesRead)
        {
            _context.TimeoutControl.BytesRead(bytesRead - _alreadyTimedBytes);
            _alreadyTimedBytes = 0;

            if (_backpressure)
            {
                _backpressure = false;
                _context.TimeoutControl.StopTimingRead();
            }
        }
    }
}
