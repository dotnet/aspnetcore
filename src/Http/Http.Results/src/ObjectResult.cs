// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    internal partial class ObjectResult : IResult
    {
        /// <summary>
        /// Creates a new <see cref="ObjectResult"/> instance with the provided <paramref name="value"/>.
        /// </summary>
        public ObjectResult(object? value, int statusCode)
        {
            Value = value;
            StatusCode = statusCode;
        }

        /// <summary>
        /// The object result.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(GetType());
            Log.ObjectResultExecuting(logger, Value);

            if (StatusCode is int statusCode)
            {
                httpContext.Response.StatusCode = statusCode;
            }

            OnFormatting(httpContext);
            return httpContext.Response.WriteAsJsonAsync(Value);
        }

        protected virtual void OnFormatting(HttpContext httpContext)
        {
        }

        private static partial class Log
        {
            public static void ObjectResultExecuting(ILogger logger, object? value)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    var valueType = value is null ? "null" : value.GetType().FullName!;
                    ObjectResultExecuting(logger, valueType);
                }
            }

            [LoggerMessage(1, LogLevel.Information, "Writing value of type '{Type}'.", EventName = "ObjectResultExecuting", SkipEnabledCheck = true)]
            public static partial void ObjectResultExecuting(ILogger logger, string type);
        }
    }
}
