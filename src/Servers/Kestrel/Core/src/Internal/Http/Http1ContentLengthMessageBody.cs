// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed class Http1ContentLengthMessageBody : Http1MessageBody
    {
        private ReadResult _readResult;
        private readonly long _contentLength;
        private long _inputLength;
        private bool _readCompleted;
        private bool _isReading;
        private int _userCanceled;
        private bool _finalAdvanceCalled;

        public Http1ContentLengthMessageBody(bool keepAlive, long contentLength, Http1Connection context)
            : base(context)
        {
            RequestKeepAlive = keepAlive;
            _contentLength = contentLength;
            _inputLength = _contentLength;
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfCompleted();
            return ReadAsyncInternal(cancellationToken);
        }

        public override async ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
        {
            if (_isReading)
            {
                throw new InvalidOperationException("Reading is already in progress.");
            }

            if (_readCompleted)
            {
                _isReading = true;
                return _readResult;
            }

            TryStart();

            // The while(true) loop is required because the Http1 connection calls CancelPendingRead to unblock
            // the call to StartTimingReadAsync to check if the request timed out.
            // However, if the user called CancelPendingRead, we want that to return a canceled ReadResult
            // We internally track an int for that.
            while (true)
            {
                // The issue is that TryRead can get a canceled read result
                // which is unknown to StartTimingReadAsync. 
                if (_context.RequestTimedOut)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
                }

                try
                {
                    var readAwaitable = _context.Input.ReadAsync(cancellationToken);

                    _isReading = true;
                    _readResult = await StartTimingReadAsync(readAwaitable, cancellationToken);
                }
                catch (ConnectionAbortedException ex)
                {
                    throw new TaskCanceledException("The request was aborted", ex);
                }

                if (_context.RequestTimedOut)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
                }

                // Make sure to handle when this is canceled here.
                if (_readResult.IsCanceled)
                {
                    if (Interlocked.Exchange(ref _userCanceled, 0) == 1)
                    {
                        // Ignore the readResult if it wasn't by the user.
                        CreateReadResultFromConnectionReadResult();

                        break;
                    }
                    else
                    {
                        // Reset the timing read here for the next call to read.
                        StopTimingRead(0);
                        continue;
                    }
                }

                var readableBuffer = _readResult.Buffer;
                var readableBufferLength = readableBuffer.Length;
                StopTimingRead(readableBufferLength);

                CheckCompletedReadResult(_readResult);

                if (readableBufferLength > 0)
                {
                    CreateReadResultFromConnectionReadResult();

                    break;
                }
            }

            return _readResult;
        }

        public override bool TryRead(out ReadResult readResult)
        {
            ThrowIfCompleted();
            return TryReadInternal(out readResult);
        }

        public override bool TryReadInternal(out ReadResult readResult)
        {
            if (_isReading)
            {
                throw new InvalidOperationException("Reading is already in progress.");
            }

            if (_readCompleted)
            {
                _isReading = true;
                readResult = _readResult;
                return true;
            }

            TryStart();

            if (!_context.Input.TryRead(out _readResult))
            {
                readResult = default;
                return false;
            }

            if (_readResult.IsCanceled)
            {
                if (Interlocked.Exchange(ref _userCanceled, 0) == 0)
                {
                    // Cancellation wasn't by the user, return default ReadResult
                    readResult = default;
                    return false;
                }
            }

            // Only set _isReading if we are returing true.
            _isReading = true;

            CreateReadResultFromConnectionReadResult();

            readResult = _readResult;
            CountBytesRead(readResult.Buffer.Length);

            return true;
        }

        public override Task ConsumeAsync()
        {
            TryStart();

            if (!_readResult.Buffer.IsEmpty && _inputLength == 0)
            {
                _context.Input.AdvanceTo(_readResult.Buffer.End);
            }

            return OnConsumeAsync();
        }

        private void CreateReadResultFromConnectionReadResult()
        {
            if (_readResult.Buffer.Length >= _inputLength + _examinedUnconsumedBytes)
            {
                _readCompleted = true;
                _readResult = new ReadResult(
                    _readResult.Buffer.Slice(0, _inputLength + _examinedUnconsumedBytes),
                    _readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 1,
                    _readCompleted);
            }

            if (_readResult.IsCompleted)
            {
                TryStop();
            }
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            if (!_isReading)
            {
                throw new InvalidOperationException("No reading operation to complete.");
            }

            _isReading = false;

            if (_readCompleted)
            {
                _readResult = new ReadResult(_readResult.Buffer.Slice(consumed, _readResult.Buffer.End), Interlocked.Exchange(ref _userCanceled, 0) == 1, _readCompleted);

                if (_readResult.Buffer.Length == 0 && !_finalAdvanceCalled)
                {
                    _context.Input.AdvanceTo(consumed);
                    _finalAdvanceCalled = true;
                    _context.OnTrailersComplete();
                }

                return;
            }

            _inputLength -= OnAdvance(_readResult, consumed, examined);
            _context.Input.AdvanceTo(consumed, examined);
        }

        protected override void OnReadStarting()
        {
            if (_contentLength > _context.MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }
        }

        public override void Complete(Exception exception)
        {
            _context.ReportApplicationError(exception);
            _completed = true;
        }

        public override void CancelPendingRead()
        {
            Interlocked.Exchange(ref _userCanceled, 1);
            _context.Input.CancelPendingRead();
        }

        protected override Task OnStopAsync()
        {
            Complete(null);
            return Task.CompletedTask;
        }
    }
}
