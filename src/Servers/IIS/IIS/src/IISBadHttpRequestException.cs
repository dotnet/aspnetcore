// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IIS
{
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
}
