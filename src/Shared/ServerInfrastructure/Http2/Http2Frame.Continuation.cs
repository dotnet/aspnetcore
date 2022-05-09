// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

/* https://tools.ietf.org/html/rfc7540#section-6.10
    +---------------------------------------------------------------+
    |                   Header Block Fragment (*)                 ...
    +---------------------------------------------------------------+
*/
#pragma warning disable CA1852 // Seal internal types
internal partial class Http2Frame
#pragma warning restore CA1852 // Seal internal types
{
    public Http2ContinuationFrameFlags ContinuationFlags
    {
        get => (Http2ContinuationFrameFlags)Flags;
        set => Flags = (byte)value;
    }

    public bool ContinuationEndHeaders => (ContinuationFlags & Http2ContinuationFrameFlags.END_HEADERS) == Http2ContinuationFrameFlags.END_HEADERS;

    public void PrepareContinuation(Http2ContinuationFrameFlags flags, int streamId)
    {
        PayloadLength = 0;
        Type = Http2FrameType.CONTINUATION;
        ContinuationFlags = flags;
        StreamId = streamId;
    }
}
