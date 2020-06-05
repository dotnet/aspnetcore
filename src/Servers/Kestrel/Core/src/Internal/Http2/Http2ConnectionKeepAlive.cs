// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal enum KeepAliveState
    {
        None,
        SendPing,
        PingSent,
        Timeout
    }

    internal class Http2ConnectionKeepAlive
    {
        internal static readonly ReadOnlySequence<byte> PingPayload = new ReadOnlySequence<byte>(new byte[8]);

        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan _keepAliveTimeout;
        private readonly ISystemClock _systemClock;
        private KeepAliveState _state;
        private bool _bytesReceivedCurrentTick;
        private long _lastBytesReceivedTimestamp;
        private long _pingSentTimestamp;

        public Http2ConnectionKeepAlive(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout, ISystemClock systemClock)
        {
            _keepAliveInterval = keepAliveInterval;
            _keepAliveTimeout = keepAliveTimeout;
            _systemClock = systemClock;
        }

        internal KeepAliveState ProcessKeepAlive(bool dataReceived)
        {
            switch (_state)
            {
                case KeepAliveState.None:
                    // No ping has been sent.
                    // If data has been received then update the timestamp
                    if (dataReceived)
                    {
                        _bytesReceivedCurrentTick = true;
                    }
                    return KeepAliveState.None;
                case KeepAliveState.SendPing:
                    // keepAliveInterval has been exceeded and a ping has been scheduled.
                    // Caller of this method will send a ping so update state to PingSent.
                    _state = KeepAliveState.PingSent;
                    _pingSentTimestamp = _systemClock.UtcNowTicks;
                    return KeepAliveState.SendPing;
                case KeepAliveState.PingSent:
                    return KeepAliveState.PingSent;
                case KeepAliveState.Timeout:
                    return KeepAliveState.Timeout;
            }

            Debug.Fail("Should never reach here.");
            return _state;
        }

        public void PingAckReceived()
        {
            if (_state == KeepAliveState.PingSent)
            {
                _pingSentTimestamp = 0;
                _state = KeepAliveState.None;
            }
        }

        public void Tick(DateTimeOffset now)
        {
            var timestamp = now.Ticks;

            // Bytes were received since the last tick.
            // Update a timestamp of when bytes were last received.
            if (_bytesReceivedCurrentTick)
            {
                _lastBytesReceivedTimestamp = timestamp;
                _bytesReceivedCurrentTick = false;
            }

            switch (_state)
            {
                case KeepAliveState.None:
                    // Check whether keep alive interval has passed since last bytes received
                    if (timestamp > (_lastBytesReceivedTimestamp + _keepAliveInterval.Ticks))
                    {
                        _state = KeepAliveState.SendPing;
                    }
                    return;
                case KeepAliveState.SendPing:
                    return;
                case KeepAliveState.PingSent:
                    if (timestamp > (_pingSentTimestamp + _keepAliveTimeout.Ticks))
                    {
                        _pingSentTimestamp = 0;
                        _state = KeepAliveState.Timeout;
                    }
                    return;
                case KeepAliveState.Timeout:
                    return;
            }

            Debug.Fail("Should never reach here.");
        }
    }
}
