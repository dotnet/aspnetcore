// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Limits for <see cref="KestrelServer"/>.
/// </summary>
public class KestrelServerLimits
{
    // Matches the non-configurable default response buffer size for Kestrel in 1.0.0
    private long? _maxResponseBufferSize = 64 * 1024;

    // Matches the default client_max_body_size in nginx.
    // Also large enough that most requests should be under the limit.
    private long? _maxRequestBufferSize = 1024 * 1024;

    // Matches the default large_client_header_buffers in nginx.
    private int _maxRequestLineSize = 8 * 1024;

    // Matches the default large_client_header_buffers in nginx.
    private int _maxRequestHeadersTotalSize = 32 * 1024;

    // Matches the default maxAllowedContentLength in IIS (~28.6 MB)
    // https://www.iis.net/configreference/system.webserver/security/requestfiltering/requestlimits#005
    private long? _maxRequestBodySize = 30000000;

    // Matches the default LimitRequestFields in Apache httpd.
    private int _maxRequestHeaderCount = 100;

    // Slightly more than SocketHttpHandler's old PooledConnectionIdleTimeout of 2 minutes.
    // https://github.com/dotnet/runtime/issues/52267
    private TimeSpan _keepAliveTimeout = TimeSpan.FromSeconds(130);

    private TimeSpan _requestHeadersTimeout = TimeSpan.FromSeconds(30);

    // Unlimited connections are allowed by default.
    private long? _maxConcurrentConnections;
    private long? _maxConcurrentUpgradedConnections;

    /// <summary>
    /// Gets or sets the maximum size of the response buffer before write
    /// calls begin to block or return tasks that don't complete until the
    /// buffer size drops below the configured limit.
    /// Defaults to 65,536 bytes (64 KB).
    /// </summary>
    /// <remarks>
    /// When set to null, the size of the response buffer is unlimited.
    /// When set to zero, all write calls will block or return tasks that
    /// don't complete until the entire response buffer is flushed.
    /// </remarks>
    public long? MaxResponseBufferSize
    {
        get => _maxResponseBufferSize;
        set
        {
            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
            }
            _maxResponseBufferSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum size of the request buffer.
    /// Defaults to 1,048,576 bytes (1 MB).
    /// </summary>
    /// <remarks>
    /// When set to null, the size of the request buffer is unlimited.
    /// </remarks>
    public long? MaxRequestBufferSize
    {
        get => _maxRequestBufferSize;
        set
        {
            if (value.HasValue && value.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveNumberOrNullRequired);
            }
            _maxRequestBufferSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed size for the HTTP request line.
    /// Defaults to 8,192 bytes (8 KB).
    /// </summary>
    /// <remarks>
    /// For HTTP/2 and HTTP/3 this measures the total size of the required pseudo headers
    /// :method, :scheme, :authority, and :path.
    /// </remarks>
    public int MaxRequestLineSize
    {
        get => _maxRequestLineSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveNumberRequired);
            }
            _maxRequestLineSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed size for the HTTP request headers.
    /// Defaults to 32,768 bytes (32 KB).
    /// </summary>
    /// <remarks>
    /// </remarks>
    public int MaxRequestHeadersTotalSize
    {
        get => _maxRequestHeadersTotalSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveNumberRequired);
            }
            _maxRequestHeadersTotalSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed number of headers per HTTP request.
    /// Defaults to 100.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public int MaxRequestHeaderCount
    {
        get => _maxRequestHeaderCount;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveNumberRequired);
            }
            _maxRequestHeaderCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed size of any request body in bytes.
    /// When set to null, the maximum request body size is unlimited.
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This can be overridden per-request via <see cref="IHttpMaxRequestBodySizeFeature"/>.
    /// Defaults to 30,000,000 bytes, which is approximately 28.6MB.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public long? MaxRequestBodySize
    {
        get => _maxRequestBodySize;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
            }
            _maxRequestBodySize = value;
        }
    }

    /// <summary>
    /// Gets or sets the keep-alive timeout.
    /// Defaults to 130 seconds.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public TimeSpan KeepAliveTimeout
    {
        get => _keepAliveTimeout;
        set
        {
            if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveTimeSpanRequired);
            }
            _keepAliveTimeout = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Gets or sets the maximum amount of time the server will spend receiving request headers.
    /// Defaults to 30 seconds.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public TimeSpan RequestHeadersTimeout
    {
        get => _requestHeadersTimeout;
        set
        {
            if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveTimeSpanRequired);
            }
            _requestHeadersTimeout = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of open connections. When set to null, the number of connections is unlimited.
    /// <para>
    /// Defaults to null.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a connection is upgraded to another protocol, such as WebSockets, its connection is counted against the
    /// <see cref="MaxConcurrentUpgradedConnections" /> limit instead of <see cref="MaxConcurrentConnections" />.
    /// </para>
    /// </remarks>
    public long? MaxConcurrentConnections
    {
        get => _maxConcurrentConnections;
        set
        {
            if (value.HasValue && value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveNumberOrNullRequired);
            }
            _maxConcurrentConnections = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of open, upgraded connections. When set to null, the number of upgraded connections is unlimited.
    /// An upgraded connection is one that has been switched from HTTP to another protocol, such as WebSockets.
    /// <para>
    /// Defaults to null.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a connection is upgraded to another protocol, such as WebSockets, its connection is counted against the
    /// <see cref="MaxConcurrentUpgradedConnections" /> limit instead of <see cref="MaxConcurrentConnections" />.
    /// </para>
    /// </remarks>
    public long? MaxConcurrentUpgradedConnections
    {
        get => _maxConcurrentUpgradedConnections;
        set
        {
            if (value.HasValue && value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
            }
            _maxConcurrentUpgradedConnections = value;
        }
    }

    internal void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteString(nameof(KeepAliveTimeout), KeepAliveTimeout.ToString());

        writer.WritePropertyName(nameof(MaxConcurrentConnections));
        if (MaxConcurrentConnections is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(MaxConcurrentConnections.Value);
        }

        writer.WritePropertyName(nameof(MaxConcurrentUpgradedConnections));
        if (MaxConcurrentUpgradedConnections is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(MaxConcurrentUpgradedConnections.Value);
        }

        writer.WritePropertyName(nameof(MaxRequestBodySize));
        if (MaxRequestBodySize is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(MaxRequestBodySize.Value);
        }

        writer.WritePropertyName(nameof(MaxRequestBufferSize));
        if (MaxRequestBufferSize is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(MaxRequestBufferSize.Value);
        }

        writer.WritePropertyName(nameof(MaxRequestHeaderCount));
        writer.WriteNumberValue(MaxRequestHeaderCount);

        writer.WritePropertyName(nameof(MaxRequestHeadersTotalSize));
        writer.WriteNumberValue(MaxRequestHeadersTotalSize);

        writer.WritePropertyName(nameof(MaxRequestLineSize));
        writer.WriteNumberValue(MaxRequestLineSize);

        writer.WritePropertyName(nameof(MaxResponseBufferSize));
        if (MaxResponseBufferSize is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(MaxResponseBufferSize.Value);
        }

        writer.WriteString(nameof(MinRequestBodyDataRate), MinRequestBodyDataRate?.ToString());
        writer.WriteString(nameof(MinResponseDataRate), MinResponseDataRate?.ToString());
        writer.WriteString(nameof(RequestHeadersTimeout), RequestHeadersTimeout.ToString());

        // HTTP2
        writer.WritePropertyName(nameof(Http2));
        writer.WriteStartObject();
        Http2.Serialize(writer);
        writer.WriteEndObject();

        // HTTP3
        writer.WritePropertyName(nameof(Http3));
        writer.WriteStartObject();
        Http3.Serialize(writer);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Limits only applicable to HTTP/2 connections.
    /// </summary>
    public Http2Limits Http2 { get; } = new Http2Limits();

    /// <summary>
    /// Limits only applicable to HTTP/3 connections.
    /// </summary>
    public Http3Limits Http3 { get; } = new Http3Limits();

    /// <summary>
    /// Gets or sets the request body minimum data rate in bytes/second.
    /// Setting this property to null indicates no minimum data rate should be enforced.
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This can be overridden per-request via <see cref="IHttpMinRequestBodyDataRateFeature"/>.
    /// Defaults to 240 bytes/second with a 5 second grace period.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public MinDataRate? MinRequestBodyDataRate { get; set; } =
        // Matches the default IIS minBytesPerSecond
        new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(5));

    /// <summary>
    /// Gets or sets the response minimum data rate in bytes/second.
    /// Setting this property to null indicates no minimum data rate should be enforced.
    /// This limit has no effect on upgraded connections which are always unlimited.
    /// This can be overridden per-request via <see cref="IHttpMinResponseDataRateFeature"/>.
    /// <para>
    /// Defaults to 240 bytes/second with a 5 second grace period.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contrary to the request body minimum data rate, this rate applies to the response status line and headers as well.
    /// </para>
    /// <para>
    /// This rate is enforced per write operation instead of being averaged over the life of the response. Whenever the server
    /// writes a chunk of data, a timer is set to the maximum of the grace period set in this property or the length of the write in
    /// bytes divided by the data rate (i.e. the maximum amount of time that write should take to complete with the specified data rate).
    /// The connection is aborted if the write has not completed by the time that timer expires.
    /// </para>
    /// </remarks>
    public MinDataRate? MinResponseDataRate { get; set; } =
        // Matches the default IIS minBytesPerSecond
        new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(5));
}
