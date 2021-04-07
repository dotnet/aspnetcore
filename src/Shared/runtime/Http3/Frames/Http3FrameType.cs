// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// HTTP3 frame types.
    /// </summary>
    /// <remarks>
    /// For frames that existed in HTTP/2, but either no longer exist or were delegated to QUIC, 7.2.8 states:
    ///     "Frame types that were used in HTTP/2 where there is no corresponding HTTP/3 frame have also been
    ///     reserved (Section 11.2.1). These frame types MUST NOT be sent, and their receipt MUST be treated
    ///     as a connection error of type H3_FRAME_UNEXPECTED."
    /// </remarks>
    internal enum Http3FrameType : long
    {
        Data = 0x0,
        Headers = 0x1,
        ReservedHttp2Priority = 0x2,
        CancelPush = 0x3,
        Settings = 0x4,
        PushPromise = 0x5,
        ReservedHttp2Ping = 0x6,
        GoAway = 0x7,
        ReservedHttp2WindowUpdate = 0x8,
        ReservedHttp2Continuation = 0x9,
        MaxPushId = 0xD
    }
}
