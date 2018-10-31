// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace MvcSandbox.AuthorizationMiddleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
            var metadata = endpoint?.Metadata?.GetMetadata<AuthorizeMetadataAttribute>();

            // Only run authorization if endpoint has metadata
            if (metadata != null)
            {
                // Check if role querystring value is a valid role
                if (!httpContext.Request.Query.TryGetValue("role", out var role) ||
                    !metadata.Roles.Contains(role.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    httpContext.Response.StatusCode = 401;
                    httpContext.Response.ContentType = "text/plain";
                    await httpContext.Response.WriteAsync($"Unauthorized access to '{endpoint.DisplayName}'.");
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}