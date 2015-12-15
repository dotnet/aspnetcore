// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class ViewComponentResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _viewComponentResultExecuting;

        static ViewComponentResultLoggerExtensions()
        {
            _viewComponentResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ViewComponentResult, running {ViewComponentName}.");
        }

        public static void ViewComponentResultExecuting(this ILogger logger, string viewComponentName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _viewComponentResultExecuting(logger, viewComponentName, null);
            }
        }

        public static void ViewComponentResultExecuting(this ILogger logger, Type viewComponentType)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _viewComponentResultExecuting(logger, viewComponentType.Name, null);
            }
        }
    }
}
