// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
