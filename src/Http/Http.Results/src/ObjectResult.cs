// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    internal partial class ObjectResult : IResult
    {
        /// <summary>
        /// Creates a new <see cref="ObjectResult"/> instance with the provided <paramref name="value"/>.
        /// </summary>
        public ObjectResult(object? value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="ObjectResult"/> instance with the provided <paramref name="value"/>.
        /// </summary>
        public ObjectResult(object? value, int? statusCode)
        {
            Value = value;
            StatusCode = statusCode;
        }

        /// <summary>
        /// The object result.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(GetType());
            Log.ObjectResultExecuting(logger, Value);

            if (Value is ProblemDetails problemDetails)
            {
                ApplyProblemDetailsDefaults(problemDetails);
            }

            if (StatusCode is { } statusCode)
            {
                httpContext.Response.StatusCode = statusCode;
            }

            OnFormatting(httpContext);
            return httpContext.Response.WriteAsJsonAsync(Value);
        }

        private void ApplyProblemDetailsDefaults(ProblemDetails problemDetails)
        {
            // We allow StatusCode to be specified either on ProblemDetails or on the ObjectResult and use it to configure the other.
            // This lets users write <c>return Conflict(new Problem("some description"))</c>
            // or <c>return Problem("some-problem", 422)</c> and have the response have consistent fields.
            if (problemDetails.Status is null)
            {
                if (StatusCode is not null)
                {
                    problemDetails.Status = StatusCode;
                }
                else
                {
                    problemDetails.Status = problemDetails is HttpValidationProblemDetails ?
                        StatusCodes.Status400BadRequest :
                        StatusCodes.Status500InternalServerError;
                }
            }

            if (StatusCode is null)
            {
                StatusCode = problemDetails.Status;
            }

            if (ProblemDetailsDefaults.Defaults.TryGetValue(problemDetails.Status.Value, out var defaults))
            {
                problemDetails.Title ??= defaults.Title;
                problemDetails.Type ??= defaults.Type;
            }
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
