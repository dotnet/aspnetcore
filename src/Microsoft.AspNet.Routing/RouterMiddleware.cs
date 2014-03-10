﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Abstractions
{
    public class RouterMiddleware
    {
        public RouterMiddleware(RequestDelegate next, IRouter route)
        {
            Next = next;
            Route = route;
        }

        private IRouter Route
        {
            get;
            set;
        }

        private RequestDelegate Next
        {
            get;
            set;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new RouteContext(httpContext)
            {
                Router = Route,
            };

            await Route.RouteAsync(context);
            if (!context.IsHandled)
            {
                await Next.Invoke(httpContext);
            }
        }
    }
}
