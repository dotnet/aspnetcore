// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Builder
{
    public class RouterMiddleware
    {
        public RouterMiddleware(RequestDelegate next, IRouter router)
        {
            Next = next;
            Router = router;
        }

        private IRouter Router
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
            var context = new RouteContext(httpContext);
            context.RouteData.Routers.Push(Router);

            await Router.RouteAsync(context);
            if (!context.IsHandled)
            {
                await Next.Invoke(httpContext);
            }
        }
    }
}
