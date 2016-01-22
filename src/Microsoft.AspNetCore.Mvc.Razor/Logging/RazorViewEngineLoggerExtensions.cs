// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class RazorViewEngineLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheMiss;
        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheHit;

        static RazorViewEngineLoggerExtensions()
        {
            _viewLookupCacheMiss = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                1,
                "View lookup cache miss for view '{ViewName}' in controller '{ControllerName}'.");

            _viewLookupCacheHit = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "View lookup cache hit for view '{ViewName}' in controller '{ControllerName}'.");
        }

        public static void ViewLookupCacheMiss(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheMiss(logger, viewName, controllerName, null);
        }

        public static void ViewLookupCacheHit(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheHit(logger, viewName, controllerName, null);
        }
    }
}
