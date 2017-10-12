// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class TreeRouterLoggerExtensions
    {
        // TreeMatcher
        private static readonly Action<ILogger, string, Exception> _requestShortCircuited = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3, "RequestShortCircuited"),
            "The current request '{RequestPath}' was short circuited.");

        private static readonly Action<ILogger, string, string, Exception> _matchedRoute;

        static TreeRouterLoggerExtensions()
        {
            _matchedRoute = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                1,
                "Request successfully matched the route with name '{RouteName}' and template '{RouteTemplate}'.");
        }

        public static void RequestShortCircuited(this ILogger logger, MatcherContext matcherContext)
        {
            var requestPath = matcherContext.HttpContext.Request.Path;
            _requestShortCircuited(logger, requestPath, null);
        }

        public static void MatchedRoute(
            this ILogger logger,
            string routeName,
            string routeTemplate)
        {
            _matchedRoute(logger, routeName, routeTemplate, null);
        }
    }
}
