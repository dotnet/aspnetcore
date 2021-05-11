// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal sealed class Http3MessageBody : MessageBody
    {
        private readonly Http3Stream _context;
        private ReadResult _readResult;

        private Http3MessageBody(Http3Stream context)
            : base(context)
        {
            _context = context;
        }

        protected override void OnReadStarting()
        {
            // Note ContentLength or MaxRequestBodySize may be null
            var maxRequestBodySize = _context.MaxRequestBodySize;

            if (_context.RequestHeaders.ContentLength > maxRequestBodySize)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge, maxRequestBodySize.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
            }
        }

        public static MessageBody For(Http3Stream context)
        {
            return new Http3MessageBody(context);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            var newlyExaminedBytes = TrackConsumedAndExaminedBytes(_readResult, consumed, examined);

            _context.RequestBodyPipe.Reader.AdvanceTo(consumed, examined);

            AddAndCheckObservedBytes(newlyExaminedBytes);
        }

        public override bool TryRead(out ReadResult readResult)
        {
            TryStartAsync();

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

        public override async ValueTask<ReadResult> ReadAtLeastAsync(int minimumSize, CancellationToken cancellationToken = default)
        {
            await TryStartAsync();

            try
            {
                var readAwaitable = _context.RequestBodyPipe.Reader.ReadAtLeastAsync(minimumSize, cancellationToken);

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

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            await TryStartAsync();

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

        public override void Complete(Exception? exception)
        {
            _context.ReportApplicationError(exception);
            _context.RequestBodyPipe.Reader.Complete();
        }

        public override ValueTask CompleteAsync(Exception? exception)
        {
            _context.ReportApplicationError(exception);
            return _context.RequestBodyPipe.Reader.CompleteAsync();
        }

        public override void CancelPendingRead()
        {
            _context.RequestBodyPipe.Reader.CancelPendingRead();
        }

        protected override ValueTask OnStopAsync()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                return default;
            }

            _context.RequestBodyPipe.Reader.Complete();

            return default;
        }
    }
}
