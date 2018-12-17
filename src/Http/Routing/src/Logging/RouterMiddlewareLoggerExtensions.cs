// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class RouterMiddlewareLoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _requestDidNotMatchRoutes;

        static RouterMiddlewareLoggerExtensions()
        {
            _requestDidNotMatchRoutes = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Request did not match any routes");
        }

        public static void RequestDidNotMatchRoutes(this ILogger logger)
        {
            _requestDidNotMatchRoutes(logger, null);
        }
    }
}
