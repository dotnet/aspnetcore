// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class JsonResultExecutorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _jsonResultExecuting;

        static JsonResultExecutorLoggerExtensions()
        {
            _jsonResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing JsonResult, writing value {Value}.");
        }

        public static void JsonResultExecuting(this ILogger logger, object value)
        {
            _jsonResultExecuting(logger, Convert.ToString(value), null);
        }
    }
}
