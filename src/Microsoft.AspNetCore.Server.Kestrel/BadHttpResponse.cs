// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public static class BadHttpResponse
    {
        internal static void ThrowException(ResponseRejectionReasons reason)
        {
            throw GetException(reason);
        }

        internal static void ThrowException(ResponseRejectionReasons reason, int value)
        {
            throw GetException(reason, value.ToString());
        }

        internal static void ThrowException(ResponseRejectionReasons reason, ResponseRejectionParameter parameter)
        {
            throw GetException(reason, parameter.ToString());
        }

        internal static InvalidOperationException GetException(ResponseRejectionReasons reason, int value)
        {
            return GetException(reason, value.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static InvalidOperationException GetException(ResponseRejectionReasons reason)
        {
            InvalidOperationException ex;
            switch (reason)
            {
                case ResponseRejectionReasons.HeadersReadonlyResponseStarted:
                    ex = new InvalidOperationException("Headers are read-only, response has already started.");
                    break;
                case ResponseRejectionReasons.OnStartingCannotBeSetResponseStarted:
                    ex = new InvalidOperationException("OnStarting cannot be set, response has already started.");
                    break;
                default:
                    ex = new InvalidOperationException("Bad response.");
                    break;
            }

            return ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetException(ResponseRejectionReasons reason, string value)
        {
            InvalidOperationException ex;
            switch (reason)
            {
                case ResponseRejectionReasons.ValueCannotBeSetResponseStarted:
                    ex = new InvalidOperationException(value + " cannot be set, response had already started.");
                    break;
                case ResponseRejectionReasons.TransferEncodingSetOnNonBodyResponse:
                    ex = new InvalidOperationException($"Transfer-Encoding set on a {value} non-body request.");
                    break;
                case ResponseRejectionReasons.WriteToNonBodyResponse:
                    ex = new InvalidOperationException($"Write to non-body {value} response.");
                    break;
                default:
                    ex = new InvalidOperationException("Bad response.");
                    break;
            }

            return ex;
        }
    }
}
