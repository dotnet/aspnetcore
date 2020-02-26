// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http
{
    /// <summary>
    /// Unidirectional stream types.
    /// </summary>
    /// <remarks>
    /// Bidirectional streams are always a request stream.
    /// </remarks>
    internal enum Http3StreamType : long
    {
        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-quic-http-24#section-6.2.1
        /// </summary>
        Control = 0x00,
        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-quic-http-24#section-6.2.2
        /// </summary>
        Push = 0x01,
        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.2
        /// </summary>
        QPackEncoder = 0x02,
        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.2
        /// </summary>
        QPackDecoder = 0x03
    }
}
