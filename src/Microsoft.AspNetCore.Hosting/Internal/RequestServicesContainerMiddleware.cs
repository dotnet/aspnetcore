// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class RequestServicesContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private IServiceScopeFactory _scopeFactory;

        public RequestServicesContainerMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (scopeFactory == null)
            {
                throw new ArgumentNullException(nameof(scopeFactory));
            }

            _scopeFactory = scopeFactory;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var existingFeature = httpContext.Features.Get<IServiceProvidersFeature>();

            // All done if request services is set
            if (existingFeature?.RequestServices != null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            using (var feature = new RequestServicesFeature(_scopeFactory))
            {
                try
                {
                    httpContext.Features.Set<IServiceProvidersFeature>(feature);
                    await _next.Invoke(httpContext);
                }
                finally
                {
                    httpContext.Features.Set(existingFeature);
                }
            }
        }
    }
}