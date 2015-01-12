// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.RequestContainer
{
    public class ContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _services;

        public ContainerMiddleware(RequestDelegate next, IServiceProvider services)
        {
            _services = services;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.RequestServices != null)
            {
                throw new Exception("TODO: nested request container scope? this is probably a mistake on your part?");
            }

            using (var container = RequestServicesContainer.EnsureRequestServices(httpContext, _services))
            {
                await _next.Invoke(httpContext);
            }
        }
    }
}
