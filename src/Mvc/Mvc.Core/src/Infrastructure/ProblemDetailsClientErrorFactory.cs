// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class ProblemDetailsClientErrorFactory : IClientErrorFactory
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private readonly ApiBehaviorOptions _options;

        public ProblemDetailsClientErrorFactory(IOptions<ApiBehaviorOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            var problemDetails = new ProblemDetails
            {
                Status = clientError.StatusCode,
                Type = "about:blank",
            };

            if (clientError.StatusCode is int statusCode &&
                _options.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;

                SetTraceId(actionContext, problemDetails);
            }

            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };
        }

        internal static void SetTraceId(ActionContext actionContext, ProblemDetails problemDetails)
        {
            var traceId = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;
            problemDetails.Extensions[TraceIdentifierKey] = traceId;
        }
    }
}
