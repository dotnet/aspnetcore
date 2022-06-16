// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

/// <summary>
/// Represets an HTTP/3 stream
/// </summary>
public interface IHttp3Stream
{
    /// <summary>
    /// The stream ID is set by QUIC.
    /// </summary>
    public long StreamId { get; }

    /// <summary>
    /// Used to track the timeout in two situations:
    /// 1. Between when the stream was started by the client, and getting a header.
    ///    Value is driven by <see cref="KestrelServerLimits.RequestHeadersTimeout"/>.
    /// 2. Between when the request delegate is complete and the transport draining.
    ///    Value is driven by <see cref="KestrelServerLimits.MinResponseDataRate"/>.
    /// </summary>
    public long StreamTimeoutTicks { get; internal set; }

    /// <summary>
    /// The stream is receiving the header frame.
    /// - Request streams = HEADERS frame.
    /// - Control streams = unidirectional stream header.
    /// </summary>
    public bool IsReceivingHeader { get; }

    /// <summary>
    /// The stream request delegate is complete and the transport is draining.
    /// </summary>
    public bool IsDraining { get; }

    /// <summary>
    /// True if this stream can be used to send data. False otherwise
    /// </summary>
    public bool IsRequestStream { get; }

    internal string TraceIdentifier { get; }

    internal void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode);

    /// <summary>
    /// Aborts the stream and stops communication over it.
    /// </summary>
    public void Abort();
}
