// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics
{
    internal static class DiagnosticsLoggerExtensions
    {
        // ExceptionHandlerMiddleware & DeveloperExceptionPageMiddleware
        private static readonly Action<ILogger, Exception> _unhandledException =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, "UnhandledException"), "An unhandled exception has occurred while executing the request.");

        // ExceptionHandlerMiddleware
        private static readonly Action<ILogger, Exception> _responseStartedErrorHandler =
            LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the error handler will not be executed.");

        private static readonly Action<ILogger, Exception> _errorHandlerException =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "Exception"), "An exception was thrown attempting to execute the error handler.");

        // DeveloperExceptionPageMiddleware
        private static readonly Action<ILogger, Exception> _responseStartedErrorPageMiddleware =
            LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the error page middleware will not be executed.");

        private static readonly Action<ILogger, Exception> _displayErrorPageException =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "DisplayErrorPageException"), "An exception was thrown attempting to display the error page.");

        public static void UnhandledException(this ILogger logger, Exception exception)
        {
            _unhandledException(logger, exception);
        }

        public static void ResponseStartedErrorHandler(this ILogger logger)
        {
            _responseStartedErrorHandler(logger, null);
        }

        public static void ErrorHandlerException(this ILogger logger, Exception exception)
        {
            _errorHandlerException(logger, exception);
        }

        public static void ResponseStartedErrorPageMiddleware(this ILogger logger)
        {
            _responseStartedErrorPageMiddleware(logger, null);
        }

        public static void DisplayErrorPageException(this ILogger logger, Exception exception)
        {
            _displayErrorPageException(logger, exception);
        }
    }
}
