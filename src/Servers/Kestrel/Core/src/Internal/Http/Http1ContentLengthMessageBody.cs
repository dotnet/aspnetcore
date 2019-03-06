// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class Http1ContentLengthMessageBody : Http1MessageBody
    {
        private readonly long _contentLength;
        private long _inputLength;
        private bool _readCompleted;
        private ReadResult _readResult;
        private bool _completed;
        private int _userCanceled;

        public Http1ContentLengthMessageBody(bool keepAlive, long contentLength, Http1Connection context)
            : base(context)
        {
            RequestKeepAlive = keepAlive;
            _contentLength = contentLength;
            _inputLength = _contentLength;
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfCompleted();

            if (_readCompleted)
            {
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

            if (_inputLength == 0)
            {
                readResult = new ReadResult(default, isCanceled: false, isCompleted: true);
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

            CreateReadResultFromConnectionReadResult();

            readResult = _readResult;

            return true;
        }

        private void ThrowIfCompleted()
        {
            if (_completed)
            {
                throw new InvalidOperationException("Reading is not allowed after the reader was completed.");
            }
        }

        private void CreateReadResultFromConnectionReadResult()
        {
            if (_readResult.Buffer.Length > _inputLength)
            {
                _readCompleted = true;
                _readResult = new ReadResult(_readResult.Buffer.Slice(0, _inputLength), _readResult.IsCanceled, _readCompleted);
            }
            else if (_readResult.Buffer.Length == _inputLength)
            {
                _readCompleted = true;
                _readResult = new ReadResult(_readResult.Buffer, _readResult.IsCanceled, _readCompleted);
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
            if (_inputLength == 0)
            {
                return;
            }

            if (_readCompleted)
            {
                _readResult = new ReadResult(_readResult.Buffer.Slice(consumed, _readResult.Buffer.End), isCanceled: false, _readCompleted);
            }

            var dataLength = _readResult.Buffer.Slice(_readResult.Buffer.Start, examined).Length;

            _inputLength -= dataLength;

            _context.Input.AdvanceTo(consumed, examined);

            OnDataRead(dataLength);
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

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            // TODO make this work with ContentLength.
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
