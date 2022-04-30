// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal enum KeepAliveState
{
    None,
    SendPing,
    PingSent,
    Timeout
}

internal sealed class Http2KeepAlive
{
    // An empty ping payload
    internal static readonly ReadOnlySequence<byte> PingPayload = new ReadOnlySequence<byte>(new byte[8]);

    private readonly TimeSpan _keepAliveInterval;
    private readonly TimeSpan _keepAliveTimeout;
    private readonly ISystemClock _systemClock;
    private long _lastFrameReceivedTimestamp;
    private long _pingSentTimestamp;

    // Internal for testing
    internal KeepAliveState _state;

    public Http2KeepAlive(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout, ISystemClock systemClock)
    {
        _keepAliveInterval = keepAliveInterval;
        _keepAliveTimeout = keepAliveTimeout;
        _systemClock = systemClock;
    }

    public KeepAliveState ProcessKeepAlive(bool frameReceived)
    {
        var timestamp = _systemClock.UtcNowTicks;

        if (frameReceived)
        {
            // System clock only has 1 second of precision, so the clock could be up to 1 second in the past.
            // To err on the side of caution, add a second to the clock when calculating the ping sent time.
            _lastFrameReceivedTimestamp = timestamp + TimeSpan.TicksPerSecond;

            // Any frame received after the keep alive interval is exceeded resets the state back to none.
            if (_state == KeepAliveState.PingSent)
            {
                _pingSentTimestamp = 0;
                _state = KeepAliveState.None;
            }
        }
        else
        {
            switch (_state)
            {
                case KeepAliveState.None:
                    // Check whether keep alive interval has passed since last frame received
                    if (timestamp > (_lastFrameReceivedTimestamp + _keepAliveInterval.Ticks))
                    {
                        // Ping will be sent immeditely after this method finishes.
                        // Set the status directly to ping sent and set the timestamp
                        _state = KeepAliveState.PingSent;
                        // System clock only has 1 second of precision, so the clock could be up to 1 second in the past.
                        // To err on the side of caution, add a second to the clock when calculating the ping sent time.
                        _pingSentTimestamp = _systemClock.UtcNowTicks + TimeSpan.TicksPerSecond;

                        // Indicate that the ping needs to be sent. This is only returned once
                        return KeepAliveState.SendPing;
                    }
                    break;
                case KeepAliveState.PingSent:
                    if (_keepAliveTimeout != TimeSpan.MaxValue)
                    {
                        if (timestamp > (_pingSentTimestamp + _keepAliveTimeout.Ticks))
                        {
                            _state = KeepAliveState.Timeout;
                        }
                    }
                    break;
            }
        }

        return _state;
    }
}
