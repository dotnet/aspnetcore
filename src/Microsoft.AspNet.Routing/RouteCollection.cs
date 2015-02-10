// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
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
        private RouteOptions _options;

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
            EnsureOptions(context.Context);

            // If we're using Best-Effort link generation then it means that we'll first look for a route where 
            // the route values are validated (context.IsBound == true). If we can't find a match like that, then
            // we'll return the path from the first route to return one.
            var useBestEffort = _options.UseBestEffortLinkGeneration;

            if (!string.IsNullOrEmpty(context.RouteName))
            {
                var isValidated = false;
                string bestPath = null;
                INamedRouter matchedNamedRoute;
                if (_namedRoutes.TryGetValue(context.RouteName, out matchedNamedRoute))
                {
                    bestPath = matchedNamedRoute.GetVirtualPath(context);
                    isValidated = context.IsBound;
                }

                // If we get here and context.IsBound == true, then we know we have a match, we want to keep
                // iterating to see if we have multiple matches.
                foreach (var unnamedRoute in _unnamedRoutes)
                {
                    // reset because we're sharing the context
                    context.IsBound = false;

                    var path = unnamedRoute.GetVirtualPath(context);
                    if (path == null)
                    {
                        continue;
                    }

                    if (bestPath != null)
                    {
                        // There was already a previous route which matched the name.
                        throw new InvalidOperationException(
                            Resources.FormatNamedRoutes_AmbiguousRoutesFound(context.RouteName));
                    }
                    else if (context.IsBound)
                    {
                        // This is the first 'validated' match that we've found.
                        bestPath = path;
                        isValidated = true;
                    }
                    else
                    {
                        Debug.Assert(bestPath == null);

                        // This is the first 'unvalidated' match that we've found.
                        bestPath = path;
                        isValidated = false;
                    }
                }

                if (isValidated || useBestEffort)
                {
                    context.IsBound = isValidated;
                    return bestPath;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string bestPath = null;
                for (var i = 0; i < Count; i++)
                {
                    var route = this[i];

                    var path = route.GetVirtualPath(context);
                    if (path == null)
                    {
                        continue;
                    }

                    if (context.IsBound)
                    {
                        // This route has validated route values, short circuit.
                        return path;
                    }
                    else if (bestPath == null)
                    {
                        // The values aren't validated, but this is the best we've seen so far
                        bestPath = path;
                    }
                }

                if (useBestEffort)
                {
                    return bestPath;
                }
                else
                {
                    return null;
                }
            }
        }

        private void EnsureLogger(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.Create<RouteCollection>();
            }
        }

        private void EnsureOptions(HttpContext context)
        {
            if (_options == null)
            {
                _options = context.RequestServices.GetRequiredService<IOptions<RouteOptions>>().Options;
            }
        }
    }
}