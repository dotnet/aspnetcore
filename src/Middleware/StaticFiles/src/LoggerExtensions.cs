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
        private static Action<ILogger, string, Exception> _methodNotSupported;
        private static Action<ILogger, string, string, Exception> _fileServed;
        private static Action<ILogger, string, Exception> _pathMismatch;
        private static Action<ILogger, string, Exception> _fileTypeNotSupported;
        private static Action<ILogger, string, Exception> _fileNotFound;
        private static Action<ILogger, string, Exception> _fileNotModified;
        private static Action<ILogger, string, Exception> _preconditionFailed;
        private static Action<ILogger, int, string, Exception> _handled;
        private static Action<ILogger, string, Exception> _rangeNotSatisfiable;
        private static Action<ILogger, StringValues, string, Exception> _sendingFileRange;
        private static Action<ILogger, StringValues, string, Exception> _copyingFileRange;
        private static Action<ILogger, Exception> _writeCancelled;
        private static Action<ILogger, Exception> _endpointMatched;

        static LoggerExtensions()
        {
            _methodNotSupported = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(1, "MethodNotSupported"),
                formatString: "{Method} requests are not supported");
            _fileServed = LoggerMessage.Define<string, string>(
               logLevel: LogLevel.Information,
               eventId: new EventId(2, "FileServed"),
               formatString: "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'");
            _pathMismatch = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(3, "PathMismatch"),
                formatString: "The request path {Path} does not match the path filter");
            _fileTypeNotSupported = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(4, "FileTypeNotSupported"),
                formatString: "The request path {Path} does not match a supported file type");
            _fileNotFound = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(5, "FileNotFound"),
                formatString: "The request path {Path} does not match an existing file");
            _fileNotModified = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: new EventId(6, "FileNotModified"),
                formatString: "The file {Path} was not modified");
            _preconditionFailed = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: new EventId(7, "PreconditionFailed"),
                formatString: "Precondition for {Path} failed");
            _handled = LoggerMessage.Define<int, string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(8, "Handled"),
                formatString: "Handled. Status code: {StatusCode} File: {Path}");
            _rangeNotSatisfiable = LoggerMessage.Define<string>(
                logLevel: LogLevel.Warning,
                eventId: new EventId(9, "RangeNotSatisfiable"),
                formatString: "Range not satisfiable for {Path}");
            _sendingFileRange = LoggerMessage.Define<StringValues, string>(
                logLevel: LogLevel.Information,
                eventId: new EventId(10, "SendingFileRange"),
                formatString: "Sending {Range} of file {Path}");
            _copyingFileRange = LoggerMessage.Define<StringValues, string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(11, "CopyingFileRange"),
                formatString: "Copying {Range} of file {Path} to the response body");
            _writeCancelled = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(14, "WriteCancelled"),
                formatString: "The file transmission was cancelled");
            _endpointMatched = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(15, "EndpointMatched"),
                formatString: "Static files was skipped as the request already matched an endpoint.");
        }

        public static void RequestMethodNotSupported(this ILogger logger, string method)
        {
            _methodNotSupported(logger, method, null);
        }

        public static void FileServed(this ILogger logger, string virtualPath, string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath))
            {
                physicalPath = "N/A";
            }
            _fileServed(logger, virtualPath, physicalPath, null);
        }

        public static void EndpointMatched(this ILogger logger)
        {
            _endpointMatched(logger, null);
        }

        public static void PathMismatch(this ILogger logger, string path)
        {
            _pathMismatch(logger, path, null);
        }

        public static void FileTypeNotSupported(this ILogger logger, string path)
        {
            _fileTypeNotSupported(logger, path, null);
        }

        public static void FileNotFound(this ILogger logger, string path)
        {
            _fileNotFound(logger, path, null);
        }

        public static void FileNotModified(this ILogger logger, string path)
        {
            _fileNotModified(logger, path, null);
        }

        public static void PreconditionFailed(this ILogger logger, string path)
        {
            _preconditionFailed(logger, path, null);
        }

        public static void Handled(this ILogger logger, int statusCode, string path)
        {
            _handled(logger, statusCode, path, null);
        }

        public static void RangeNotSatisfiable(this ILogger logger, string path)
        {
            _rangeNotSatisfiable(logger, path, null);
        }

        public static void SendingFileRange(this ILogger logger, StringValues range, string path)
        {
            _sendingFileRange(logger, range, path, null);
        }

        public static void CopyingFileRange(this ILogger logger, StringValues range, string path)
        {
            _copyingFileRange(logger, range, path, null);
        }

        public static void WriteCancelled(this ILogger logger, Exception ex)
        {
            _writeCancelled(logger, ex);
        }
    }
}
