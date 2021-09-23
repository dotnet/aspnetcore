// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal static partial class HttpLoggingExtensions
    {
        public static void RequestLog(this ILogger logger, HttpRequestLog requestLog, LogLevel logLevel) => logger.Log(
            logLevel,
            new EventId(1, "RequestLogLog"),
            requestLog,
            exception: null,
            formatter: HttpRequestLog.Callback);
        public static void ResponseLog(this ILogger logger, HttpResponseLog responseLog, LogLevel logLevel) => logger.Log(
            logLevel,
            new EventId(2, "ResponseLog"),
            responseLog,
            exception: null,
            formatter: HttpResponseLog.Callback);

        [LoggerMessage(EventId = 3, Message ="RequestBody: {Body}", EventName = "RequestBody")]
        public static partial void RequestBody(this ILogger logger, string body, LogLevel logLevel);

        [LoggerMessage(EventId = 4, Message = "ResponseBody: {Body}", EventName = "ResponseBody")]
        public static partial void ResponseBody(this ILogger logger, string body, LogLevel logLevel);

        [LoggerMessage(5, LogLevel.Debug, "Decode failure while converting body.", EventName = "DecodeFailure")]
        public static partial void DecodeFailure(this ILogger logger, Exception ex);

        [LoggerMessage(6, LogLevel.Debug, "Unrecognized Content-Type for body.", EventName = "UnrecognizedMediaType")]
        public static partial void UnrecognizedMediaType(this ILogger logger);
    }
}
