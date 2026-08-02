// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3ConnectionErrorException : Exception
{
    public Http3ConnectionErrorException(string message, Http3ErrorCode errorCode, ConnectionEndReason reason)
        : base($"HTTP/3 connection error ({Http3Formatting.ToFormattedErrorCode(errorCode)}): {message}")
    {
        ErrorCode = errorCode;
        Reason = reason;
    }

    public Http3ErrorCode ErrorCode { get; }
    public ConnectionEndReason Reason { get; }
}
