// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class RouterMiddlewareLoggerExtensions
    {
        private static readonly Action<ILogger, Exception?> _requestNotMatched;

        static RouterMiddlewareLoggerExtensions()
        {
            _requestNotMatched = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "RequestNotMatched"),
                "Request did not match any routes");
        }

        public static void RequestNotMatched(this ILogger logger)
        {
            _requestNotMatched(logger, null);
        }
    }
}
