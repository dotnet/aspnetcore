using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ViewResultExecutorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _viewFound;
        private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _viewNotFound;

        static ViewResultExecutorLoggerExtensions()
        {
            _viewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "The view '{ViewName}' was found.");
            _viewNotFound = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Error,
                2,
                "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}");
        }

        public static void ViewFound(this ILogger logger, string viewName)
        {
            _viewFound(logger, viewName, null);
        }

        public static void ViewNotFound(
            this ILogger logger,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            _viewNotFound(logger, viewName, searchedLocations, null);
        }
    }
}
