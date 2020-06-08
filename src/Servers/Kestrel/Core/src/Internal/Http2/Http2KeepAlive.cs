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

    internal class Http2KeepAlive
    {
        // An empty ping payload
        internal static readonly ReadOnlySequence<byte> PingPayload = new ReadOnlySequence<byte>(new byte[8]);

        private readonly object _lock = new object();
        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan? _keepAliveTimeout;
        private readonly ISystemClock _systemClock;
        private bool _bytesReceivedCurrentTick;
        private long _lastBytesReceivedTimestamp;
        private long _pingSentTimestamp;

        // Internal for testing
        internal KeepAliveState _state;

        public Http2KeepAlive(TimeSpan keepAliveInterval, TimeSpan? keepAliveTimeout, ISystemClock systemClock)
        {
            _keepAliveInterval = keepAliveInterval;
            _keepAliveTimeout = keepAliveTimeout;
            _systemClock = systemClock;
        }

        internal KeepAliveState ProcessKeepAlive(bool dataReceived)
        {
            lock (_lock)
            {
                if (dataReceived)
                {
                    _bytesReceivedCurrentTick = true;

                    // Any data received after the keep alive interval is exceeded
                    // resets the state back to none.
                    if (_state == KeepAliveState.PingSent)
                    {
                        _pingSentTimestamp = 0;
                        _state = KeepAliveState.None;
                    }
                }

                return _state;
            }
        }

        public void PingSent()
        {
            lock (_lock)
            {
                _state = KeepAliveState.PingSent;

                // System clock only has 1 second of precision, so the clock could be up to 1 second in the past.
                // To err on the side of caution, add a second to the clock when calculating the ping sent time.
                _pingSentTimestamp = _systemClock.UtcNowTicks + TimeSpan.TicksPerSecond;
            }
        }

        public void Tick(DateTimeOffset now)
        {
            var timestamp = now.Ticks;

            lock (_lock)
            {
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
                        if (_keepAliveTimeout != null)
                        {
                            if (timestamp > (_pingSentTimestamp + _keepAliveTimeout.GetValueOrDefault().Ticks))
                            {
                                _state = KeepAliveState.Timeout;
                            }
                        }
                        return;
                    case KeepAliveState.Timeout:
                        return;
                }
            }

            Debug.Fail("Should never reach here.");
        }
    }
}
