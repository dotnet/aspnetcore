// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2StreamErrorException : Exception
{
    public Http2StreamErrorException(int streamId, string message, Http2ErrorCode errorCode)
        : base($"HTTP/2 stream ID {streamId} error ({errorCode}): {message}")
    {
        StreamId = streamId;
        ErrorCode = errorCode;
    }

    public int StreamId { get; }

    public Http2ErrorCode ErrorCode { get; }
}
