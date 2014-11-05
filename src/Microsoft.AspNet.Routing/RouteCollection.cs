// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollection : IRouteCollection
    {
        private readonly List<IRouter> _routes = new List<IRouter>();
        private readonly List<IRouter> _unnamedRoutes = new List<IRouter>();
        private readonly Dictionary<string, INamedRouter> _namedRoutes =
                                    new Dictionary<string, INamedRouter>(StringComparer.OrdinalIgnoreCase);
        private ILogger _logger;

        public IRouter this[int index]
        {
            get { return _routes[index]; }
        }

        public int Count
        {
            get { return _routes.Count; }
        }

        public void Add([NotNull] IRouter router)
        {
            var namedRouter = router as INamedRouter;
            if (namedRouter != null)
            {
                if (!string.IsNullOrEmpty(namedRouter.Name))
                {
                    _namedRoutes.Add(namedRouter.Name, namedRouter);
                }
            }
            else
            {
                _unnamedRoutes.Add(router);
            }

            _routes.Add(router);
        }

        public async virtual Task RouteAsync(RouteContext context)
        {
            EnsureLogger(context.HttpContext);
            using (_logger.BeginScope("RouteCollection.RouteAsync"))
            {
                for (var i = 0; i < Count; i++)
                {
                    var route = this[i];

                    var oldRouteData = context.RouteData;

                    var newRouteData = new RouteData(oldRouteData);
                    newRouteData.Routers.Add(route);

                    try
                    {
                        context.RouteData = newRouteData;

                        await route.RouteAsync(context);
                        if (context.IsHandled)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        if (!context.IsHandled)
                        {
                            context.RouteData = oldRouteData;
                        }
                    }
                }

                if (_logger.IsEnabled(LogLevel.Verbose))
                {
                    _logger.WriteValues(new RouteCollectionRouteAsyncValues()
                    {
                        Handled = context.IsHandled,
                        Routes = _routes
                    });
                }
            }
        }

        public virtual string GetVirtualPath(VirtualPathContext context)
        {
            if (!string.IsNullOrEmpty(context.RouteName))
            {
                INamedRouter matchedNamedRoute;
                _namedRoutes.TryGetValue(context.RouteName, out matchedNamedRoute);

                var virtualPath = matchedNamedRoute != null ? matchedNamedRoute.GetVirtualPath(context) : null;
                foreach (var unnamedRoute in _unnamedRoutes)
                {
                    var tempVirtualPath = unnamedRoute.GetVirtualPath(context);
                    if (tempVirtualPath != null)
                    {
                        if (virtualPath != null)
                        {
                            // There was already a previous route which matched the name.
                            throw new InvalidOperationException(
                                        Resources.FormatNamedRoutes_AmbiguousRoutesFound(context.RouteName));
                        }

                        virtualPath = tempVirtualPath;
                    }
                }

                return virtualPath;
            }
            else
            {
                for (var i = 0; i < Count; i++)
                {
                    var route = this[i];

                    var path = route.GetVirtualPath(context);
                    if (path != null)
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private void EnsureLogger(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.Create<RouteCollection>();
            }
        }
    }
}