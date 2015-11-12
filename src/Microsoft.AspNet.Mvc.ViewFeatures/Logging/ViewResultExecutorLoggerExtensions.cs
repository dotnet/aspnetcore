// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class ViewResultExecutorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _viewResultExecuting;
        private static readonly Action<ILogger, string, Exception> _viewFound;
        private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _viewNotFound;

        static ViewResultExecutorLoggerExtensions()
        {
            _viewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ViewResult, running view at path {Path}.");
            _viewFound = LoggerMessage.Define<string>(
                LogLevel.Verbose,
                2,
                "The view '{ViewName}' was found.");
            _viewNotFound = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Error,
                3,
                "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}");
        }

        public static void ViewResultExecuting(this ILogger logger, IView view)
        {
            _viewResultExecuting(logger, view.Path, null);
        }

        public static void ViewFound(this ILogger logger, string viewName)
        {
            _viewFound(logger, viewName, null);
        }

        public static void ViewNotFound(this ILogger logger, string viewName,
            IEnumerable<string> searchedLocations)
        {
            _viewNotFound(logger, viewName, searchedLocations, null);
        }
    }
}
