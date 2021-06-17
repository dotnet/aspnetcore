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
    internal static partial class LoggerExtensions
    {
        [LoggerMessage(EventId = 1, EventName = "MethodNotSupported", Level = LogLevel.Debug, Message = "{Method} requests are not supported")]
        public static partial void RequestMethodNotSupported(this ILogger logger, string method);

        [LoggerMessage(EventId = 2, EventName = "FileServed", Level = LogLevel.Debug, Message = "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'")]
        public static partial void FileServed(this ILogger logger, string virtualPath, string physicalPath);

        [LoggerMessage(EventId = 15, EventName = "EndpointMatched", Level = LogLevel.Debug, Message = "Static files was skipped as the request already matched an endpoint.")]
        public static partial void EndpointMatched(this ILogger logger);

        [LoggerMessage(EventId = 3, EventName = "PathMismatch", Level = LogLevel.Debug, Message = "The request path {Path} does not match the path filter")]
        public static partial void PathMismatch(this ILogger logger, string path);

        [LoggerMessage(EventId = 4, EventName = "FileTypeNotSupported", Level = LogLevel.Debug, Message = "The request path {Path} does not match a supported file type")]
        public static partial void FileTypeNotSupported(this ILogger logger, string path);

        [LoggerMessage(EventId = 5, EventName = "FileNotFound", Level = LogLevel.Debug, Message = "The request path {Path} does not match an existing file")]
        public static partial void FileNotFound(this ILogger logger, string path);

        [LoggerMessage(EventId = 6, EventName = "FileNotModified", Level = LogLevel.Information, Message = "The file {Path} was not modified")]
        public static partial void FileNotModified(this ILogger logger, string path);

        [LoggerMessage(EventId = 7, EventName = "PreconditionFailed", Level = LogLevel.Information, Message = "Precondition for {Path} failed")]
        public static partial void PreconditionFailed(this ILogger logger, string path);

        [LoggerMessage(EventId = 8, EventName = "Handled", Level = LogLevel.Debug, Message = "Handled. Status code: {StatusCode} File: {Path}")]
        public static partial void Handled(this ILogger logger, int statusCode, string path);

        [LoggerMessage(EventId = 9, EventName = "RangeNotSatisfiable", Level = LogLevel.Warning, Message = "Range not satisfiable for {Path}")]
        public static partial void RangeNotSatisfiable(this ILogger logger, string path);

        [LoggerMessage(EventId = 10, EventName = "SendingFileRange", Level = LogLevel.Information, Message = "Sending {Range} of file {Path}")]
        public static partial void SendingFileRange(this ILogger logger, StringValues range, string path);

        [LoggerMessage(EventId = 11, EventName = "CopyingFileRange", Level = LogLevel.Debug, Message = "Copying {Range} of file {Path} to the response body")]
        public static partial void CopyingFileRange(this ILogger logger, StringValues range, string path);

        [LoggerMessage(EventId = 14, EventName = "WriteCancelled", Level = LogLevel.Debug, Message = "The file transmission was cancelled")]
        public static partial void WriteCancelled(this ILogger logger, Exception ex);
    }
}
