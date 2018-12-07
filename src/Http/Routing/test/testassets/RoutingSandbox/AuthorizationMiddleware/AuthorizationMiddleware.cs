// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace RoutingSample.Web.AuthorizationMiddleware
{
    public class AuthorizationMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(ILogger<AuthorizationMiddleware> logger, RequestDelegate next)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
            if (endpoint != null)
            {
                var metadata = endpoint.Metadata.GetMetadata<AuthorizationMetadata>();
                // Only run authorization if endpoint has metadata
                if (metadata != null)
                {
                    if (!httpContext.Request.Query.TryGetValue("x-role", out var role) ||
                        !metadata.Roles.Contains(role.ToString()))
                    {
                        httpContext.Response.StatusCode = 401;
                        httpContext.Response.ContentType = "text/plain";
                        await httpContext.Response.WriteAsync($"Unauthorized access to '{endpoint.DisplayName}'.");
                        return;
                    }
                }
            }

            await _next(httpContext);
        }
    }
}
