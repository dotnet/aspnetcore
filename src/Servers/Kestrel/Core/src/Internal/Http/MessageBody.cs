// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal abstract class MessageBody
    {
        private static readonly MessageBody _zeroContentLengthClose = new ZeroContentLengthMessageBody(keepAlive: false);
        private static readonly MessageBody _zeroContentLengthKeepAlive = new ZeroContentLengthMessageBody(keepAlive: true);

        private readonly HttpProtocol _context;

        private bool _send100Continue = true;
        private long _consumedBytes;
        private bool _stopped;

        protected bool _timingEnabled;
        protected bool _backpressure;
        protected long _alreadyTimedBytes;
        protected long _examinedUnconsumedBytes;

        protected MessageBody(HttpProtocol context)
        {
            _context = context;
        }

        public static MessageBody ZeroContentLengthClose => _zeroContentLengthClose;

        public static MessageBody ZeroContentLengthKeepAlive => _zeroContentLengthKeepAlive;

        public bool RequestKeepAlive { get; protected set; }

        public bool RequestUpgrade { get; protected set; }

        public virtual bool IsEmpty => false;

        protected IKestrelTrace Log => _context.ServiceContext.Log;

        public abstract void AdvanceTo(SequencePosition consumed);

        public abstract void AdvanceTo(SequencePosition consumed, SequencePosition examined);

        public abstract bool TryRead(out ReadResult readResult);

        public abstract void Complete(Exception exception);

        public abstract void CancelPendingRead();

        public abstract ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default);

        public virtual Task ConsumeAsync()
        {
            TryStart();

            return OnConsumeAsync();
        }

        public virtual Task StopAsync()
        {
            TryStop();

            return OnStopAsync();
        }

        protected virtual Task OnConsumeAsync() => Task.CompletedTask;

        protected virtual Task OnStopAsync() => Task.CompletedTask;

        public virtual void Reset()
        {
            _send100Continue = true;
            _consumedBytes = 0;
            _stopped = false;
            _timingEnabled = false;
            _backpressure = false;
            _alreadyTimedBytes = 0;
            _examinedUnconsumedBytes = 0;
        }

        protected void TryProduceContinue()
        {
            if (_send100Continue)
            {
                _context.HttpResponseControl.ProduceContinue();
                _send100Continue = false;
            }
        }

        protected void TryStart()
        {
            if (_context.HasStartedConsumingRequestBody)
            {
                return;
            }

            OnReadStarting();
            _context.HasStartedConsumingRequestBody = true;

            if (!RequestUpgrade)
            {
                // Accessing TraceIdentifier will lazy-allocate a string ID.
                // Don't access TraceIdentifer unless logging is enabled.
                if (Log.IsEnabled(LogLevel.Debug))
                {
                    Log.RequestBodyStart(_context.ConnectionIdFeature, _context.TraceIdentifier);
                }

                if (_context.MinRequestBodyDataRate != null)
                {
                    _timingEnabled = true;
                    _context.TimeoutControl.StartRequestBody(_context.MinRequestBodyDataRate);
                }
            }

            OnReadStarted();
        }

        protected void TryStop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;

            if (!RequestUpgrade)
            {
                // Accessing TraceIdentifier will lazy-allocate a string ID
                // Don't access TraceIdentifer unless logging is enabled.
                if (Log.IsEnabled(LogLevel.Debug))
                {
                    Log.RequestBodyDone(_context.ConnectionIdFeature, _context.TraceIdentifier);
                }

                if (_timingEnabled)
                {
                    if (_backpressure)
                    {
                        _context.TimeoutControl.StopTimingRead();
                    }

                    _context.TimeoutControl.StopRequestBody();
                }
            }
        }

        protected virtual void OnReadStarting()
        {
        }

        protected virtual void OnReadStarted()
        {
        }

        protected virtual void OnDataRead(long bytesRead)
        {
        }

        protected void AddAndCheckConsumedBytes(long consumedBytes)
        {
            _consumedBytes += consumedBytes;

            if (_consumedBytes > _context.MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }
        }

        protected ValueTask<ReadResult> StartTimingReadAsync(ValueTask<ReadResult> readAwaitable, CancellationToken cancellationToken)
        {

            if (!readAwaitable.IsCompleted && _timingEnabled)
            {
                TryProduceContinue();

                _backpressure = true;
                _context.TimeoutControl.StartTimingRead();
            }

            return readAwaitable;
        }

        protected void CountBytesRead(long bytesInReadResult)
        {
            var numFirstSeenBytes = bytesInReadResult - _alreadyTimedBytes;

            if (numFirstSeenBytes > 0)
            {
                _context.TimeoutControl.BytesRead(numFirstSeenBytes);
            }
        }

        protected void StopTimingRead(long bytesInReadResult)
        {
            CountBytesRead(bytesInReadResult);

            if (_backpressure)
            {
                _backpressure = false;
                _context.TimeoutControl.StopTimingRead();
            }
        }

        protected long OnAdvance(ReadResult readResult, SequencePosition consumed, SequencePosition examined)
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

            long examinedLength, consumedLength, totalLength;

            if (consumed.Equals(examined))
            {
                examinedLength = readResult.Buffer.Slice(readResult.Buffer.Start, examined).Length;
                consumedLength = examinedLength;
            }
            else
            {
                consumedLength = readResult.Buffer.Slice(readResult.Buffer.Start, consumed).Length;
                examinedLength = consumedLength + readResult.Buffer.Slice(consumed, examined).Length;
            }

            if (examined.Equals(readResult.Buffer.End))
            {
                totalLength = examinedLength;
            }
            else
            {
                totalLength = readResult.Buffer.Length;
            }

            var newlyExamined = examinedLength - _examinedUnconsumedBytes;

            if (newlyExamined > 0)
            {
                OnDataRead(newlyExamined);
                _examinedUnconsumedBytes += newlyExamined;
            }

            _examinedUnconsumedBytes -= consumedLength;
            _alreadyTimedBytes = totalLength - consumedLength;

            return newlyExamined;
        }
    }
}
