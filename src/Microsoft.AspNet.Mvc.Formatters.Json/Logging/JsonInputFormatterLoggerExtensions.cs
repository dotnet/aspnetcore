using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Formatters.Json.Logging
{
    internal static class JsonInputFormatterLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _jsonInputFormatterCrashed;

        static JsonInputFormatterLoggerExtensions()
        {
            _jsonInputFormatterCrashed = LoggerMessage.Define<string>(
                LogLevel.Verbose,
                1,
                "JSON input formatter threw an exception.");
        }

        public static void JsonInputException(this ILogger logger, Exception exception)
        {
            _jsonInputFormatterCrashed(logger, exception.ToString(), exception);
        }
    }
}
