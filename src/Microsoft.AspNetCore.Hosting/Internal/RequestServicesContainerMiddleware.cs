// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class RequestServicesContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

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

            _next = next;
            _scopeFactory = scopeFactory;
        }

        public Task Invoke(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);

            var features = httpContext.Features;
            var servicesFeature = features.Get<IServiceProvidersFeature>();

            // All done if RequestServices is set
            if (servicesFeature?.RequestServices != null)
            {
                return _next.Invoke(httpContext);
            }

            features.Set<IServiceProvidersFeature>(new RequestServicesFeature(httpContext, _scopeFactory));
            return _next.Invoke(httpContext);
        }
    }
}