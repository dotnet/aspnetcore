// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json.Internal
{
    internal static class MvcJsonLoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _jsonInputFormatterCrashed;

        private static readonly Action<ILogger, string, Exception> _jsonResultExecuting;

        static MvcJsonLoggerExtensions()
        {
            _jsonInputFormatterCrashed = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "JSON input formatter threw an exception.");

            _jsonResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing JsonResult, writing value type {Value}.");
        }

        public static void JsonInputException(this ILogger logger, Exception exception)
        {
            _jsonInputFormatterCrashed(logger, exception);
        }

        public static void JsonResultExecuting(this ILogger logger, object value)
        {
            var stringValue = value == null ? "null" : Convert.ToString(value.GetType());
            _jsonResultExecuting(logger, stringValue, null);
        }
    }
}
