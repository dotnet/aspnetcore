// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class RouterMiddlewareLoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _requestNotMatched;

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
