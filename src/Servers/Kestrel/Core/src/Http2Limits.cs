// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Limits only applicable to HTTP/2 connections.
/// </summary>
public class Http2Limits
{
    private int _maxStreamsPerConnection = 100;
    private int _headerTableSize = (int)Http2PeerSettings.DefaultHeaderTableSize;
    private int _maxFrameSize = (int)Http2PeerSettings.DefaultMaxFrameSize;
    private int _maxRequestHeaderFieldSize = 32 * 1024; // Matches MaxRequestHeadersTotalSize
    private int _initialConnectionWindowSize = 1024 * 1024; // Equal to SocketTransportOptions.MaxReadBufferSize and larger than any one single stream.
    private int _initialStreamWindowSize = 768 * 1024; // Larger than the default 64kb and able to use most (3/4ths) of the connection window by itself.
    private TimeSpan _keepAlivePingDelay = TimeSpan.MaxValue;
    private TimeSpan _keepAlivePingTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Limits the number of concurrent request streams per HTTP/2 connection. Excess streams will be refused.
    /// <para>
    /// Value must be greater than 0, defaults to 100 streams.
    /// </para>
    /// </summary>
    public int MaxStreamsPerConnection
    {
        get => _maxStreamsPerConnection;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
            }

            _maxStreamsPerConnection = value;
        }
    }

    /// <summary>
    /// Limits the size of the header compression tables, in octets, the HPACK encoder and decoder on the server can use.
    /// <para>
    /// Value must be greater than or equal to 0, defaults to 4096 octets (4 KiB).
    /// </para>
    /// </summary>
    public int HeaderTableSize
    {
        get => _headerTableSize;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanOrEqualToZeroRequired);
            }

            _headerTableSize = value;
        }
    }

    /// <summary>
    /// Indicates the size of the largest frame payload that is allowed to be received, in octets. The size must be between 2^14 and 2^24-1.
    /// <para>
    /// Value must be between 2^14 and 2^24, defaults to 2^14 octets (16 KiB).
    /// </para>
    /// </summary>
    public int MaxFrameSize
    {
        get => _maxFrameSize;
        set
        {
            if (value < Http2PeerSettings.MinAllowedMaxFrameSize || value > Http2PeerSettings.MaxAllowedMaxFrameSize)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.FormatArgumentOutOfRange(Http2PeerSettings.MinAllowedMaxFrameSize, Http2PeerSettings.MaxAllowedMaxFrameSize));
            }

            _maxFrameSize = value;
        }
    }

    /// <summary>
    /// Indicates the size of the maximum allowed size of a request header field sequence, in octets. This limit applies to both name and value sequences in their compressed and uncompressed representations.
    /// <para>
    /// Value must be greater than 0, defaults to 2^14 octets (16 KiB).
    /// </para>
    /// </summary>
    public int MaxRequestHeaderFieldSize
    {
        get => _maxRequestHeaderFieldSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
            }

            _maxRequestHeaderFieldSize = value;
        }
    }

    /// <summary>
    /// Indicates how much request body data, in bytes, the server is willing to receive and buffer at a time aggregated across all
    /// requests (streams) per connection. Note requests are also limited by <see cref="InitialStreamWindowSize"/>
    /// <para>
    /// Value must be greater than or equal to 64 KiB and less than 2 GiB, defaults to 1 MiB.
    /// </para>
    /// </summary>
    public int InitialConnectionWindowSize
    {
        get => _initialConnectionWindowSize;
        set
        {
            if (value < Http2PeerSettings.DefaultInitialWindowSize || value > Http2PeerSettings.MaxWindowSize)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    CoreStrings.FormatArgumentOutOfRange(Http2PeerSettings.DefaultInitialWindowSize, Http2PeerSettings.MaxWindowSize));
            }

            _initialConnectionWindowSize = value;
        }
    }

    /// <summary>
    /// Indicates how much request body data, in bytes, the server is willing to receive and buffer at a time per stream.
    /// Note connections are also limited by <see cref="InitialConnectionWindowSize"/>. There must be space in both the stream
    /// window and connection window for a client to upload request body data.
    /// <para>
    /// Value must be greater than or equal to 64 KiB and less than 2 GiB, defaults to 768 KiB.
    /// </para>
    /// </summary>
    public int InitialStreamWindowSize
    {
        get => _initialStreamWindowSize;
        set
        {
            if (value < Http2PeerSettings.DefaultInitialWindowSize || value > Http2PeerSettings.MaxWindowSize)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    CoreStrings.FormatArgumentOutOfRange(Http2PeerSettings.DefaultInitialWindowSize, Http2PeerSettings.MaxWindowSize));
            }

            _initialStreamWindowSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the keep alive ping delay. The server will send a keep alive ping to the client if it
    /// doesn't receive any frames on a connection for this period of time. This property is used together with
    /// <see cref="KeepAlivePingTimeout"/> to close broken connections.
    /// <para>
    /// Delay value must be greater than or equal to 1 second. Set to <see cref="TimeSpan.MaxValue"/> to
    /// disable the keep alive ping.
    /// Defaults to <see cref="TimeSpan.MaxValue"/>.
    /// </para>
    /// </summary>
    public TimeSpan KeepAlivePingDelay
    {
        get => _keepAlivePingDelay;
        set
        {
            // Keep alive uses Kestrel's system clock which has a 1 second resolution. Time is greater or equal to clock resolution.
            if (value < Heartbeat.Interval && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.FormatArgumentTimeSpanGreaterOrEqual(Heartbeat.Interval));
            }

            _keepAlivePingDelay = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Gets or sets the keep alive ping timeout. Keep alive pings are sent when a period of inactivity exceeds
    /// the configured <see cref="KeepAlivePingDelay"/> value. The server will close the connection if it
    /// doesn't receive any frames within the timeout.
    /// <para>
    /// Timeout must be greater than or equal to 1 second. Set to <see cref="TimeSpan.MaxValue"/> to
    /// disable the keep alive ping timeout.
    /// Defaults to 20 seconds.
    /// </para>
    /// </summary>
    public TimeSpan KeepAlivePingTimeout
    {
        get => _keepAlivePingTimeout;
        set
        {
            // Keep alive uses Kestrel's system clock which has a 1 second resolution. Time is greater or equal to clock resolution.
            if (value < Heartbeat.Interval && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.FormatArgumentTimeSpanGreaterOrEqual(Heartbeat.Interval));
            }

            _keepAlivePingTimeout = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    }

    internal void Serialize(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(nameof(MaxStreamsPerConnection));
        writer.WriteNumberValue(MaxStreamsPerConnection);

        writer.WritePropertyName(nameof(HeaderTableSize));
        writer.WriteNumberValue(HeaderTableSize);

        writer.WritePropertyName(nameof(MaxFrameSize));
        writer.WriteNumberValue(MaxFrameSize);

        writer.WritePropertyName(nameof(MaxRequestHeaderFieldSize));
        writer.WriteNumberValue(MaxRequestHeaderFieldSize);

        writer.WritePropertyName(nameof(InitialConnectionWindowSize));
        writer.WriteNumberValue(InitialConnectionWindowSize);

        writer.WritePropertyName(nameof(InitialStreamWindowSize));
        writer.WriteNumberValue(InitialStreamWindowSize);

        writer.WriteString(nameof(KeepAlivePingDelay), KeepAlivePingDelay.ToString());
        writer.WriteString(nameof(KeepAlivePingTimeout), KeepAlivePingTimeout.ToString());
    }
}
