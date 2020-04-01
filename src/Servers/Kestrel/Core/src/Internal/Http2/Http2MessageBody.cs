// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2MessageBody : MessageBody
    {
        private readonly Http2Stream _context;
        private ReadResult _readResult;

        public Http2MessageBody(Http2Stream context)
            : base(context)
        {
            _context = context;
        }

        protected override void OnReadStarting()
        {
            // Note ContentLength or MaxRequestBodySize may be null
            if (_context.RequestHeaders.ContentLength > _context.MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }
        }

        protected override void OnReadStarted()
        {
            // Produce 100-continue if no request body data for the stream has arrived yet.
            if (!_context.RequestBodyStarted)
            {
                TryProduceContinue();
            }
        }

        protected override void OnDataRead(long bytesRead)
        {
            // The HTTP/2 flow control window cannot be larger than 2^31-1 which limits bytesRead.
            _context.OnDataRead((int)bytesRead);
            AddAndCheckConsumedBytes(bytesRead);
        }

        public override void Reset()
        {
            base.Reset();
            _readResult = default;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            OnAdvance(_readResult, consumed, examined);
            _context.RequestBodyPipe.Reader.AdvanceTo(consumed, examined);
        }

        public override bool TryRead(out ReadResult readResult)
        {
            TryStart();

            var hasResult = _context.RequestBodyPipe.Reader.TryRead(out readResult);

            if (hasResult)
            {
                _readResult = readResult;

                CountBytesRead(readResult.Buffer.Length);

                if (readResult.IsCompleted)
                {
                    TryStop();
                }
            }

            return hasResult;
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            TryStart();

            try
            {
                var readAwaitable = _context.RequestBodyPipe.Reader.ReadAsync(cancellationToken);

                _readResult = await StartTimingReadAsync(readAwaitable, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }

            StopTimingRead(_readResult.Buffer.Length);

            if (_readResult.IsCompleted)
            {
                TryStop();
            }

            return _readResult;
        }

        public override void Complete(Exception exception)
        {
            _context.RequestBodyPipe.Reader.Complete();
            _context.ReportApplicationError(exception);
        }

        public override void CancelPendingRead()
        {
            _context.RequestBodyPipe.Reader.CancelPendingRead();
        }

        protected override Task OnStopAsync()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                return Task.CompletedTask;
            }

            _context.RequestBodyPipe.Reader.Complete();

            return Task.CompletedTask;
        }
    }
}
