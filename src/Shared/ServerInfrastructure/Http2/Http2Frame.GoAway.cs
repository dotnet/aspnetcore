// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

/* https://tools.ietf.org/html/rfc7540#section-6.8
    +-+-------------------------------------------------------------+
    |R|                  Last-Stream-ID (31)                        |
    +-+-------------------------------------------------------------+
    |                      Error Code (32)                          |
    +---------------------------------------------------------------+
    |                  Additional Debug Data (*)                    |
    +---------------------------------------------------------------+
*/
internal partial class Http2Frame
{
    public int GoAwayLastStreamId { get; set; }

    public Http2ErrorCode GoAwayErrorCode { get; set; }

    public void PrepareGoAway(int lastStreamId, Http2ErrorCode errorCode)
    {
        PayloadLength = 8;
        Type = Http2FrameType.GOAWAY;
        Flags = 0;
        StreamId = 0;
        GoAwayLastStreamId = lastStreamId;
        GoAwayErrorCode = errorCode;
    }
}
