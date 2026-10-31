// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS;

///<inheritdoc/>
[Obsolete("Moved to Microsoft.AspNetCore.Http.BadHttpRequestException. See https://aka.ms/badhttprequestexception for details.")] // Never remove.
public sealed class BadHttpRequestException : Microsoft.AspNetCore.Http.BadHttpRequestException
{
    internal BadHttpRequestException(string message, int statusCode, RequestRejectionReason reason)
        : base(message, statusCode)
    {
        Reason = reason;
    }

    ///<inheritdoc/>
    public new int StatusCode { get => base.StatusCode; }

    internal RequestRejectionReason Reason { get; }
}
