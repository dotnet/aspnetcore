// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal interface IHttp3Stream
{
    /// <summary>
    /// The stream ID is set by QUIC.
    /// </summary>
    long StreamId { get; }

    /// <summary>
    /// Used to track the timeout in two situations:
    /// 1. Between when the stream was started by the client, and getting a header.
    ///    Value is driven by <see cref="KestrelServerLimits.RequestHeadersTimeout"/>.
    /// 2. Between when the request delegate is complete and the transport draining.
    ///    Value is driven by <see cref="KestrelServerLimits.MinResponseDataRate"/>.
    /// </summary>
    long StreamTimeoutTimestamp { get; set; }

    /// <summary>
    /// The stream is receiving the header frame.
    /// - Request streams = HEADERS frame.
    /// - Control streams = unidirectional stream header.
    /// </summary>
    bool IsReceivingHeader { get; }

    /// <summary>
    /// The stream request delegate is complete and the transport is draining.
    /// </summary>
    bool IsDraining { get; }

    bool IsRequestStream { get; }

    bool EndStreamReceived { get; }
    bool IsAborted { get; }
    bool IsCompleted { get; }

    string TraceIdentifier { get; }

    void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode);
}
