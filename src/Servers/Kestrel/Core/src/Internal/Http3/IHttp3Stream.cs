// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal interface IHttp3Stream
    {
        /// <summary>
        /// The stream ID is set by QUIC.
        /// </summary>
        long StreamId { get; }

        /// <summary>
        /// Used to track the timeout between when the stream was started by the client, and getting a header.
        /// Value is driven by <see cref="KestrelServerLimits.RequestHeadersTimeout"/>.
        /// </summary>
        long HeaderTimeoutTicks { get; set; }

        /// <summary>
        /// The stream has received and parsed the header frame.
        /// - Request streams = HEADERS frame.
        /// - Control streams = unidirectional stream header.
        /// </summary>
        bool ReceivedHeader { get; }

        bool IsRequestStream { get; }

        void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode);
    }
}
