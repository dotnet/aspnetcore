// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RequestServicesContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _services;

        public RequestServicesContainerMiddleware([NotNull] RequestDelegate next, [NotNull] IServiceProvider services)
        {
            _services = services;
            _next = next;
        }

        public async Task Invoke([NotNull] HttpContext httpContext)
        {
            // All done if there request services is set
            if (httpContext.RequestServices != null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            var priorApplicationServices = httpContext.ApplicationServices;
            var serviceProvider = priorApplicationServices ?? _services;
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            try
            {
                // Creates the scope and temporarily swap services
                using (var scope = scopeFactory.CreateScope())
                {
                    httpContext.ApplicationServices = serviceProvider;
                    httpContext.RequestServices = scope.ServiceProvider;

                    await _next.Invoke(httpContext);
                }
            }
            finally
            {
                httpContext.RequestServices = null;
                httpContext.ApplicationServices = priorApplicationServices;
            }
        }
    }
}