// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IIS;

internal static class IISBadHttpRequestException
{
    internal static void Throw(RequestRejectionReason reason)
    {
        throw GetException(reason);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CS0618 // Type or member is obsolete
    internal static BadHttpRequestException GetException(RequestRejectionReason reason)
    {
        BadHttpRequestException ex;
        switch (reason)
        {
            case RequestRejectionReason.RequestBodyTooLarge:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestBodyTooLarge, StatusCodes.Status413PayloadTooLarge, reason);
                break;
            default:
                ex = new BadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest, reason);
                break;
        }
        return ex;
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
