// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal static class HttpLoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception?> _requestBody =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "RequestBody"), "RequestBody: {Body}");

        private static readonly Action<ILogger, string, Exception?> _responseBody =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, "ResponseBody"), "ResponseBody: {Body}");

        private static readonly Action<ILogger, Exception?> _decodeFailure =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, "DecodeFaulure"), "Decode failure while converting body.");

        private static readonly Action<ILogger, Exception?> _unrecognizedMediaType =
            LoggerMessage.Define(LogLevel.Debug, new EventId(6, "UnrecognizedMediaType"), "Unrecognized Content-Type for body.");

        public static void RequestLog(this ILogger logger, HttpRequestLog requestLog) => logger.Log(
            LogLevel.Information,
            new EventId(1, "RequestLog"),
            requestLog,
            exception: null,
            formatter: HttpRequestLog.Callback);
        public static void ResponseLog(this ILogger logger, HttpResponseLog responseLog) => logger.Log(
            LogLevel.Information,
            new EventId(2, "ResponseLog"),
            responseLog,
            exception: null,
            formatter: HttpResponseLog.Callback);
        public static void RequestBody(this ILogger logger, string body) => _requestBody(logger, body, null);
        public static void ResponseBody(this ILogger logger, string body) => _responseBody(logger, body, null);
        public static void DecodeFailure(this ILogger logger, Exception ex) => _decodeFailure(logger, ex);
        public static void UnrecognizedMediaType(this ILogger logger) => _unrecognizedMediaType(logger, null);
    }
}
