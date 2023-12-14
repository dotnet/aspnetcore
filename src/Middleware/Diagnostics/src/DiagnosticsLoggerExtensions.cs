// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics;

internal static partial class DiagnosticsLoggerExtensions
{
    // ExceptionHandlerMiddleware & DeveloperExceptionPageMiddleware
    [LoggerMessage(1, LogLevel.Error, "An unhandled exception has occurred while executing the request.", EventName = "UnhandledException")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Debug, "The request was aborted by the client.", EventName = "RequestAborted")]
    public static partial void RequestAbortedException(this ILogger logger);

    // ExceptionHandlerMiddleware
    [LoggerMessage(2, LogLevel.Warning, "The response has already started, the error handler will not be executed.", EventName = "ResponseStarted")]
    public static partial void ResponseStartedErrorHandler(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "An exception was thrown attempting to execute the error handler.", EventName = "Exception")]
    public static partial void ErrorHandlerException(this ILogger logger, Exception exception);
}

internal static partial class DeveloperExceptionPageMiddlewareLoggerExtensions
{
    [LoggerMessage(2, LogLevel.Warning, "The response has already started, the error page middleware will not be executed.", EventName = "ResponseStarted")]
    public static partial void ResponseStartedErrorPageMiddleware(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "An exception was thrown attempting to display the error page.", EventName = "DisplayErrorPageException")]
    public static partial void DisplayErrorPageException(this ILogger logger, Exception exception);
}
