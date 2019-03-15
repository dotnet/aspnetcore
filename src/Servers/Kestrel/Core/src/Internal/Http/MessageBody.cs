// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal abstract class MessageBody
    {
        private static readonly MessageBody _zeroContentLengthClose = new ZeroContentLengthMessageBody(keepAlive: false);
        private static readonly MessageBody _zeroContentLengthKeepAlive = new ZeroContentLengthMessageBody(keepAlive: true);

        private readonly HttpProtocol _context;
        private readonly MinDataRate _minRequestBodyDataRate;

        private bool _send100Continue = true;
        private long _consumedBytes;
        private bool _stopped;

        protected bool _timingEnabled;
        protected bool _backpressure;
        protected long _alreadyTimedBytes;

        protected MessageBody(HttpProtocol context, MinDataRate minRequestBodyDataRate)
        {
            _context = context;
            _minRequestBodyDataRate = minRequestBodyDataRate;
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

        public abstract void OnWriterCompleted(Action<Exception, object> callback, object state);

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
                Log.RequestBodyStart(_context.ConnectionIdFeature, _context.TraceIdentifier);

                if (_minRequestBodyDataRate != null)
                {
                    _timingEnabled = true;
                    _context.TimeoutControl.StartRequestBody(_minRequestBodyDataRate);
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
                Log.RequestBodyDone(_context.ConnectionIdFeature, _context.TraceIdentifier);

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

        protected void StopTimingRead(long bytesRead)
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
