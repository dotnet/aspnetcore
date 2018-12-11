// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticFiles
{
    /// <summary>
    /// Defines *all* the logger messages produced by static files
    /// </summary>
    internal static class LoggerExtensions
    {
        private static Action<ILogger, string, Exception> _logMethodNotSupported;
        private static Action<ILogger, string, string, Exception> _logFileServed;
        private static Action<ILogger, string, Exception> _logPathMismatch;
        private static Action<ILogger, string, Exception> _logFileTypeNotSupported;
        private static Action<ILogger, string, Exception> _logFileNotFound;
        private static Action<ILogger, string, Exception> _logPathNotModified;
        private static Action<ILogger, string, Exception> _logPreconditionFailed;
        private static Action<ILogger, int, string, Exception> _logHandled;
        private static Action<ILogger, string, Exception> _logRangeNotSatisfiable;
        private static Action<ILogger, StringValues, string, Exception> _logSendingFileRange;
        private static Action<ILogger, StringValues, string, Exception> _logCopyingFileRange;
        private static Action<ILogger, long, string, string, Exception> _logCopyingBytesToResponse;
        private static Action<ILogger, Exception> _logWriteCancelled;

        static LoggerExtensions()
        {
            _logMethodNotSupported = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: 1,
                formatString: "{Method} requests are not supported");
            _logFileServed = LoggerMessage.Define<string, string>(
               logLevel: LogLevel.Information,
               eventId: 2,
               formatString: "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'");
            _logPathMismatch = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: 3,
                formatString: "The request path {Path} does not match the path filter");
            _logFileTypeNotSupported = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: 4,
                formatString: "The request path {Path} does not match a supported file type");
            _logFileNotFound = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: 5,
                formatString: "The request path {Path} does not match an existing file");
            _logPathNotModified = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: 6,
                formatString: "The file {Path} was not modified");
            _logPreconditionFailed = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: 7,
                formatString: "Precondition for {Path} failed");
            _logHandled = LoggerMessage.Define<int, string>(
                logLevel: LogLevel.Debug,
                eventId: 8,
                formatString: "Handled. Status code: {StatusCode} File: {Path}");
            _logRangeNotSatisfiable = LoggerMessage.Define<string>(
                logLevel: LogLevel.Warning,
                eventId: 9,
                formatString: "Range not satisfiable for {Path}");
            _logSendingFileRange = LoggerMessage.Define<StringValues, string>(
                logLevel: LogLevel.Information,
                eventId: 10,
                formatString: "Sending {Range} of file {Path}");
            _logCopyingFileRange = LoggerMessage.Define<StringValues, string>(
                logLevel: LogLevel.Debug,
                eventId: 11,
                formatString: "Copying {Range} of file {Path} to the response body");
            _logCopyingBytesToResponse = LoggerMessage.Define<long, string, string>(
                logLevel: LogLevel.Debug,
                eventId: 12,
                formatString: "Copying bytes {Start}-{End} of file {Path} to response body");
            _logWriteCancelled = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 14,
                formatString: "The file transmission was cancelled");
        }

        public static void LogRequestMethodNotSupported(this ILogger logger, string method)
        {
            _logMethodNotSupported(logger, method, null);
        }

        public static void LogFileServed(this ILogger logger, string virtualPath, string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath))
            {
                physicalPath = "N/A";
            }
            _logFileServed(logger, virtualPath, physicalPath, null);
        }

        public static void LogPathMismatch(this ILogger logger, string path)
        {
            _logPathMismatch(logger, path, null);
        }

        public static void LogFileTypeNotSupported(this ILogger logger, string path)
        {
            _logFileTypeNotSupported(logger, path, null);
        }

        public static void LogFileNotFound(this ILogger logger, string path)
        {
            _logFileNotFound(logger, path, null);
        }

        public static void LogPathNotModified(this ILogger logger, string path)
        {
            _logPathNotModified(logger, path, null);
        }

        public static void LogPreconditionFailed(this ILogger logger, string path)
        {
            _logPreconditionFailed(logger, path, null);
        }

        public static void LogHandled(this ILogger logger, int statusCode, string path)
        {
            _logHandled(logger, statusCode, path, null);
        }

        public static void LogRangeNotSatisfiable(this ILogger logger, string path)
        {
            _logRangeNotSatisfiable(logger, path, null);
        }

        public static void LogSendingFileRange(this ILogger logger, StringValues range, string path)
        {
            _logSendingFileRange(logger, range, path, null);
        }

        public static void LogCopyingFileRange(this ILogger logger, StringValues range, string path)
        {
            _logCopyingFileRange(logger, range, path, null);
        }

        public static void LogCopyingBytesToResponse(this ILogger logger, long start, long? end, string path)
        {
            _logCopyingBytesToResponse(
                logger,
                start,
                end != null ? end.ToString() : "*",
                path,
                null);
        }

        public static void LogWriteCancelled(this ILogger logger, Exception ex)
        {
            _logWriteCancelled(logger, ex);
        }
    }
}
