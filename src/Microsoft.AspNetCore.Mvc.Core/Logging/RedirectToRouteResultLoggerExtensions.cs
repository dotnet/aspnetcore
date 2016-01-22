// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class RedirectToRouteResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception> _redirectToRouteResultExecuting;

        static RedirectToRouteResultLoggerExtensions()
        {
            _redirectToRouteResultExecuting = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                1,
                "Executing RedirectToRouteResult, redirecting to {Destination} from route {RouteName}.");
        }

        public static void RedirectToRouteResultExecuting(this ILogger logger, string destination, string routeName)
        {
            _redirectToRouteResultExecuting(logger, destination, routeName, null);
        }
    }
}
