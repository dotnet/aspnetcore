using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class InnerAttributeRouteLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception> _matchedRouteName;

        static InnerAttributeRouteLoggerExtensions()
        {
            _matchedRouteName = LoggerMessage.Define<string, string>(
                LogLevel.Verbose,
                1,
                "Request successfully matched the route with name '{RouteName}' and template '{RouteTemplate}'.");
        }

        public static void MatchedRouteName(
            this ILogger logger,
            string routeName,
            string routeTemplate)
        {
            _matchedRouteName(logger, routeName, routeTemplate, null);
        }
    }
}
