// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    // Think this is close to good
    public class ForContentLength : Http1MessageBody
    {
        private readonly long _contentLength;
        private long _inputLength;
        private ReadResult _readResult; // TODO we can probably make this in Http1MessageBody or even MessageBody
        private bool _completed;
        private int _userCanceled;

        public ForContentLength(bool keepAlive, long contentLength, Http1Connection context)
            : base(context)
        {
            RequestKeepAlive = keepAlive;
            _contentLength = contentLength;
            _inputLength = _contentLength;
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Reading is not allowed after the reader was completed.");
            }

            if (_inputLength == 0)
            {
                _readResult = new ReadResult(default, isCanceled: false, isCompleted: true);
                return _readResult;
            }

            TryStart();

            while (true)
            {
                // This isn't great. The issue is that TryRead can get a canceled read result
                // which is unknown to StartTimingReadAsync. 
                if (_context.RequestTimedOut)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
                }

                _readResult = await StartTimingReadAsync(cancellationToken);

                if (_context.RequestTimedOut)
                {
                    Debug.Assert(_readResult.IsCanceled);
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
                }

                if (_readResult.IsCanceled)
                {
                    if (Interlocked.CompareExchange(ref _userCanceled, 0, 1) == 1)
                    {
                        // Ignore the readResult if it wasn't by the user.
                        break;
                    }
                }

                var readableBuffer = _readResult.Buffer;
                var readableBufferLength = readableBuffer.Length;
                StopTimingRead(readableBufferLength);

                if (_readResult.IsCompleted)
                {
                    // OnInputOrOutputCompleted() is an idempotent method that closes the connection. Sometimes
                    // input completion is observed here before the Input.OnWriterCompleted() callback is fired,
                    // so we call OnInputOrOutputCompleted() now to prevent a race in our tests where a 400
                    // response is written after observing the unexpected end of request content instead of just
                    // closing the connection without a response as expected.
                    _context.OnInputOrOutputCompleted();

                    BadHttpRequestException.Throw(RequestRejectionReason.UnexpectedEndOfRequestContent);
                }

                if (readableBufferLength > 0)
                {
                    break;
                }
            }

            // handle cases where we send more data than the content length
            if (_readResult.Buffer.Length > _inputLength)
            {
                _readResult = new ReadResult(_readResult.Buffer.Slice(0, _inputLength), _readResult.IsCanceled, isCompleted: true);

            }
            else if (_readResult.Buffer.Length == _inputLength)
            {
                _readResult = new ReadResult(_readResult.Buffer, _readResult.IsCanceled, isCompleted: true);
            }

            if (_readResult.IsCompleted)
            {
                TryStop();
            }

            return _readResult;
        }

        public override bool TryRead(out ReadResult readResult)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Reading is not allowed after the reader was completed.");
            }

            if (_inputLength == 0)
            {
                readResult = new ReadResult(default, isCanceled: false, isCompleted: true);
                return true;
            }

            TryStart();

            var boolResult = _context.Input.TryRead(out _readResult);

            if (_readResult.Buffer.Length > _inputLength)
            {
                _readResult = new ReadResult(_readResult.Buffer.Slice(0, _inputLength), _readResult.IsCanceled, isCompleted: true);

            }
            else if (_readResult.Buffer.Length == _inputLength)
            {
                _readResult = new ReadResult(_readResult.Buffer, _readResult.IsCanceled, isCompleted: true);
            }

            readResult = _readResult;

            if (_readResult.IsCompleted)
            {
                TryStop();
            }

            return boolResult;
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

            var dataLength = _readResult.Buffer.Slice(_readResult.Buffer.Start, consumed).Length;
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

        private ValueTask<ReadResult> StartTimingReadAsync(CancellationToken cancellationToken)
        {
            var readAwaitable = _context.Input.ReadAsync(cancellationToken);

            if (!readAwaitable.IsCompleted && _timingEnabled)
            {
                TryProduceContinue();

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

        protected override Task OnConsumeAsync()
        {
            try
            {
                if (TryRead(out var readResult))
                {
                    AdvanceTo(readResult.Buffer.End);

                    if (readResult.IsCompleted)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TryRead can throw OperationCanceledException https://github.com/dotnet/corefx/issues/32029
                // because of buggy logic, this works around that for now
            }
            catch (BadHttpRequestException ex)
            {
                // At this point, the response has already been written, so this won't result in a 4XX response;
                // however, we still need to stop the request processing loop and log.
                _context.SetBadRequestState(ex);
                return Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var connectionAbortedException = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication, ex);
                _context.ReportApplicationError(connectionAbortedException);

                // Have to abort the connection because we can't finish draining the request
                _context.StopProcessingNextRequest();
                return Task.CompletedTask;
            }

            return OnConsumeAsyncAwaited();
        }

        private async Task OnConsumeAsyncAwaited()
        {
            Log.RequestBodyNotEntirelyRead(_context.ConnectionIdFeature, _context.TraceIdentifier);

            _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout.Ticks, TimeoutReason.RequestBodyDrain);

            try
            {
                ReadResult result;
                do
                {
                    result = await ReadAsync();
                    AdvanceTo(result.Buffer.End);
                } while (!result.IsCompleted);
            }
            catch (BadHttpRequestException ex)
            {
                _context.SetBadRequestState(ex);
            }
            catch (ConnectionAbortedException)
            {
                Log.RequestBodyDrainTimedOut(_context.ConnectionIdFeature, _context.TraceIdentifier);
            }
            catch (InvalidOperationException ex)
            {
                var connectionAbortedException = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication, ex);
                _context.ReportApplicationError(connectionAbortedException);

                // Have to abort the connection because we can't finish draining the request
                _context.StopProcessingNextRequest();
            }
            finally
            {
                _context.TimeoutControl.CancelTimeout();
            }
        }
    }
}
