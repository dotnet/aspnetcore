// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class EndpointMiddleware
    {
        private RequestDelegate _next;

        public EndpointMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var feature = context.Features.Get<IDispatcherFeature>();
            if (feature.RequestDelegate == null)
            {
                await _next(context);
            }
            else
            {
                await feature.RequestDelegate(context);
            }
        }
    }
}
