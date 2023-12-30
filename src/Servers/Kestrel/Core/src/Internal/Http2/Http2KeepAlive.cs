// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

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

    private readonly long _keepAliveInterval;
    private readonly long _keepAliveTimeout;
    private readonly TimeProvider _timeProvider;
    private long _lastFrameReceivedTimestamp;
    private long _pingSentTimestamp;

    // Internal for testing
    internal KeepAliveState _state;

    public Http2KeepAlive(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout, TimeProvider timeProvider)
    {
        _keepAliveInterval = keepAliveInterval.ToTicks(timeProvider);
        _keepAliveTimeout = keepAliveTimeout == TimeSpan.MaxValue ? long.MaxValue
            : keepAliveTimeout.ToTicks(timeProvider);
        _timeProvider = timeProvider;
    }

    public KeepAliveState ProcessKeepAlive(bool frameReceived)
    {
        var timestamp = _timeProvider.GetTimestamp();

        if (frameReceived)
        {
            // To err on the side of caution, add a second to the time when calculating the ping sent time.
            _lastFrameReceivedTimestamp = timestamp + _timeProvider.TimestampFrequency;

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
                    if (timestamp > (_lastFrameReceivedTimestamp + _keepAliveInterval))
                    {
                        // Ping will be sent immeditely after this method finishes.
                        // Set the status directly to ping sent and set the timestamp
                        _state = KeepAliveState.PingSent;
                        // To err on the side of caution, add a second to the time when calculating the ping sent time.
                        _pingSentTimestamp = timestamp + _timeProvider.TimestampFrequency;

                        // Indicate that the ping needs to be sent. This is only returned once
                        return KeepAliveState.SendPing;
                    }
                    break;
                case KeepAliveState.PingSent:
                    if (_keepAliveTimeout != long.MaxValue)
                    {
                        if (timestamp > (_pingSentTimestamp + _keepAliveTimeout))
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
