// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Logging
{
    internal static class PartialViewResultExecutorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _partialViewFound;
        private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _partialViewNotFound;
        private static readonly Action<ILogger, string, Exception> _partialViewResultExecuting;

        static PartialViewResultExecutorLoggerExtensions()
        {
            _partialViewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing PartialViewResult, running view at path {Path}.");
            _partialViewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                2,
                "The partial view '{PartialViewName}' was found.");
            _partialViewNotFound = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Error,
                3,
                "The partial view '{PartialViewName}' was not found. Searched locations: {SearchedViewLocations}");
        }

        public static void PartialViewFound(
            this ILogger logger,
            string partialViewName)
        {
            _partialViewFound(logger, partialViewName, null);
        }

        public static void PartialViewNotFound(
            this ILogger logger,
            string partialViewName,
            IEnumerable<string> searchedLocations)
        {
            _partialViewNotFound(logger, partialViewName, searchedLocations, null);
        }

        public static void PartialViewResultExecuting(this ILogger logger, IView view)
        {
            _partialViewResultExecuting(logger, view.Path, null);
        }
    }
}
