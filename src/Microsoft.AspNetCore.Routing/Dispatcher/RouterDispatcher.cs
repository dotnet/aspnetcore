// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    /// <summary>
    /// An adapter to plug an <see cref="IRouter"/> into a dispatcher.
    /// </summary>
    public class RouterDispatcher : DispatcherBase
    {
        private readonly IRouter _router;

        public RouterDispatcher(IRouter router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            _router = router;
        }

        public async override Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var routeContext = new RouteContext(httpContext);
            await _router.RouteAsync(routeContext);
        }
    }
}

