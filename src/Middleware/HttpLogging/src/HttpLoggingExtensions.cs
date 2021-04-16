// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal static class HttpLoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception?> _requestBody =
            LoggerMessage.Define<string>(LogLevel.Information, LoggerEventIds.RequestBody, "RequestBody: {Body}");

        private static readonly Action<ILogger, string, Exception?> _responseBody =
            LoggerMessage.Define<string>(LogLevel.Information, LoggerEventIds.ResponseBody, "ResponseBody: {Body}");

        private static readonly Action<ILogger, Exception?> _decodeFailure =
            LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.DecodeFailure, "Decode failure while converting body.");

        public static void RequestBody(this ILogger logger, string body) => _requestBody(logger, body, null);
        public static void ResponseBody(this ILogger logger, string body) => _responseBody(logger, body, null);
        public static void DecodeFailure(this ILogger logger, Exception ex) => _decodeFailure(logger, ex);
    }
}
