// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Builder
{
    public class RouterMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly IRouter _router;
        private readonly IServiceProvider _services;

        public RouterMiddleware(
            RequestDelegate next,
            IServiceProvider services,
            ILoggerFactory loggerFactory,
            IRouter router)
        {
            _next = next;
            _services = services;
            _router = router;

            _logger = loggerFactory.Create<RouterMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            using (RequestServicesContainer.EnsureRequestServices(httpContext, _services))
            {
                using (_logger.BeginScope("RouterMiddleware.Invoke"))
                {
                    var context = new RouteContext(httpContext);
                    context.RouteData.Routers.Add(_router);

                    await _router.RouteAsync(context);

                    if (_logger.IsEnabled(LogLevel.Verbose))
                    {
                        _logger.WriteValues(new RouterMiddlewareInvokeValues() { Handled = context.IsHandled });
                    }

                    if (!context.IsHandled)
                    {
                        await _next.Invoke(httpContext);
                    }
                }
            }
        }
    }
}
