// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2ConnectionErrorException : Exception
{
    public Http2ConnectionErrorException(string message, Http2ErrorCode errorCode, ConnectionEndReason reason)
        : base($"HTTP/2 connection error ({errorCode}): {message}")
    {
        ErrorCode = errorCode;
        Reason = reason;
    }

    public Http2ErrorCode ErrorCode { get; }
    public ConnectionEndReason Reason { get; }
}
