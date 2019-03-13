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
    public class Http2MessageBody : MessageBody
    {
        private readonly Http2Stream _context;
        private ReadResult _readResult;
        private long _alreadyExaminedInNextReadResult;

        private Http2MessageBody(Http2Stream context, MinDataRate minRequestBodyDataRate)
            : base(context, minRequestBodyDataRate)
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

        public static MessageBody For(Http2Stream context, MinDataRate minRequestBodyDataRate)
        {
            if (context.ReceivedEmptyRequestBody)
            {
                return ZeroContentLengthClose;
            }

            return new Http2MessageBody(context, minRequestBodyDataRate);
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            // This code path is fairly hard to understand so let's break it down with an example
            // ReadAsync returns a ReadResult of length 50.
            // Advance(25, 40). The examined length would be 40 and consumed length would be 25.
            // _totalExaminedInPreviousReadResult starts at 0. newlyExamined is 40.
            // OnDataRead is called with length 40.
            // _totalExaminedInPreviousReadResult is now 40 - 25 = 15.

            // The next call to ReadAsync returns 50 again
            // Advance(5, 5) is called
            // newlyExamined is 5 - 15, or -10.
            // Update _totalExaminedInPreviousReadResult to 10 as we consumed 5.

            // The next call to ReadAsync returns 50 again
            // _totalExaminedInPreviousReadResult is 10
            // Advance(50, 50) is called
            // newlyExamined = 50 - 10 = 40
            // _totalExaminedInPreviousReadResult is now 50
            // _totalExaminedInPreviousReadResult is finally 0 after subtracting consumedLength.

            long examinedLength;
            long consumedLength;
            if (consumed.Equals(examined))
            {
                examinedLength = _readResult.Buffer.Slice(_readResult.Buffer.Start, examined).Length;
                consumedLength = examinedLength;
            }
            else 
            {
                consumedLength = _readResult.Buffer.Slice(_readResult.Buffer.Start, consumed).Length;
                examinedLength = consumedLength + _readResult.Buffer.Slice(consumed, examined).Length;
            }

            _context.RequestBodyPipe.Reader.AdvanceTo(consumed, examined);
            
            var newlyExamined = examinedLength - _alreadyExaminedInNextReadResult;

            if (newlyExamined > 0)
            {
                OnDataRead(newlyExamined);
                _alreadyExaminedInNextReadResult += newlyExamined;
            }

            _alreadyExaminedInNextReadResult -= consumedLength;
        }

        public override bool TryRead(out ReadResult readResult)
        {
            var result = _context.RequestBodyPipe.Reader.TryRead(out readResult);
            _readResult = readResult;

            return result;
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

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            _context.RequestBodyPipe.Reader.OnWriterCompleted(callback, state);
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
