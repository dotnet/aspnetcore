// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RequestServicesContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _services;

        public RequestServicesContainerMiddleware(RequestDelegate next, IServiceProvider services)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
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

            using (var feature = new RequestServicesFeature(_services))
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