// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using System;

namespace Microsoft.AspNet.Builder
{
    public class RouterMiddleware
    {
        private ILogger _logger;

        public RouterMiddleware(RequestDelegate next, IServiceProvider services, IRouter router)
        {
            Next = next;
            Router = router;
            Services = services;
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

        private IServiceProvider Services
        {
            get;
            set;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            using (RequestServicesContainer.EnsureRequestServices(httpContext, Services))
            {
                EnsureLogger(httpContext);
                using (_logger.BeginScope("RouterMiddleware.Invoke"))
                {
                    var context = new RouteContext(httpContext);
                    context.RouteData.Routers.Add(Router);

                    await Router.RouteAsync(context);

                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new RouterMiddlewareInvokeValues() { Handled = context.IsHandled });
                    }

                    if (!context.IsHandled)
                    {
                        await Next.Invoke(httpContext);
                    }
                }
            }
        }

        private void EnsureLogger(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetService<ILoggerFactory>();
                _logger = factory.Create<RouterMiddleware>();
            }
        }
    }
}
