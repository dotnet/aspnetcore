// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IIS
{
    [Obsolete("Moved to Microsoft.AspNetCore.Http.BadHttpRequestException")]
    public sealed class BadHttpRequestException : Microsoft.AspNetCore.Http.BadHttpRequestException
    {
        private BadHttpRequestException(string message, int statusCode, RequestRejectionReason reason)
            : base(message, statusCode)
        {
            Reason = reason;
        }

        internal RequestRejectionReason Reason { get; }

        internal static void Throw(RequestRejectionReason reason)
        {
            throw GetException(reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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
    }
}
