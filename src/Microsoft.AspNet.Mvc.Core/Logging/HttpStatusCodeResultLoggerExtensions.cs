// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class HttpStatusCodeLoggerExtensions
    {
        private static readonly Action<ILogger, int, Exception> _httpStatusCodeResultExecuting;

        static HttpStatusCodeLoggerExtensions()
        {
            _httpStatusCodeResultExecuting = LoggerMessage.Define<int>(
                LogLevel.Information,
                1,
                "Executing HttpStatusCodeResult, setting HTTP status code {StatusCode}");
        }

        public static void HttpStatusCodeResultExecuting(this ILogger logger, int statusCode)
        {
            _httpStatusCodeResultExecuting(logger, statusCode, null);
        }
    }
}
