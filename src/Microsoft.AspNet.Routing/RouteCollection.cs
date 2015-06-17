// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollection : IRouteCollection
    {
        private readonly List<IRouter> _routes = new List<IRouter>();
        private readonly List<IRouter> _unnamedRoutes = new List<IRouter>();
        private readonly Dictionary<string, INamedRouter> _namedRoutes =
                                    new Dictionary<string, INamedRouter>(StringComparer.OrdinalIgnoreCase);

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
        }

        public virtual VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            EnsureOptions(context.Context);

            // If we're using Best-Effort link generation then it means that we'll first look for a route where
            // the route values are validated (context.IsBound == true). If we can't find a match like that, then
            // we'll return the path from the first route to return one.
            var useBestEffort = _options.UseBestEffortLinkGeneration;

            if (!string.IsNullOrEmpty(context.RouteName))
            {
                var isValidated = false;
                VirtualPathData bestPathData = null;
                INamedRouter matchedNamedRoute;
                if (_namedRoutes.TryGetValue(context.RouteName, out matchedNamedRoute))
                {
                    bestPathData = matchedNamedRoute.GetVirtualPath(context);
                    isValidated = context.IsBound;
                }

                // If we get here and context.IsBound == true, then we know we have a match, we want to keep
                // iterating to see if we have multiple matches.
                foreach (var unnamedRoute in _unnamedRoutes)
                {
                    // reset because we're sharing the context
                    context.IsBound = false;

                    var pathData = unnamedRoute.GetVirtualPath(context);
                    if (pathData == null)
                    {
                        continue;
                    }

                    if (bestPathData != null)
                    {
                        // There was already a previous route which matched the name.
                        throw new InvalidOperationException(
                            Resources.FormatNamedRoutes_AmbiguousRoutesFound(context.RouteName));
                    }
                    else if (context.IsBound)
                    {
                        // This is the first 'validated' match that we've found.
                        bestPathData = pathData;
                        isValidated = true;
                    }
                    else
                    {
                        Debug.Assert(bestPathData == null);

                        // This is the first 'unvalidated' match that we've found.
                        bestPathData = pathData;
                        isValidated = false;
                    }
                }

                if (isValidated || useBestEffort)
                {
                    context.IsBound = isValidated;

                    if (bestPathData != null)
                    {
                        bestPathData = new VirtualPathData(
                            bestPathData.Router,
                            NormalizeVirtualPath(bestPathData.VirtualPath),
                            bestPathData.DataTokens);
                    }

                    return bestPathData;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                VirtualPathData bestPathData = null;
                for (var i = 0; i < Count; i++)
                {
                    var route = this[i];

                    var pathData = route.GetVirtualPath(context);
                    if (pathData == null)
                    {
                        continue;
                    }

                    if (context.IsBound)
                    {
                        // This route has validated route values, short circuit.
                        return new VirtualPathData(
                            pathData.Router,
                            NormalizeVirtualPath(pathData.VirtualPath),
                            pathData.DataTokens);
                    }
                    else if (bestPathData == null)
                    {
                        // The values aren't validated, but this is the best we've seen so far
                        bestPathData = pathData;
                    }
                }

                if (useBestEffort)
                {
                    return new VirtualPathData(
                        bestPathData.Router,
                        NormalizeVirtualPath(bestPathData.VirtualPath),
                        bestPathData.DataTokens);
                }
                else
                {
                    return null;
                }
            }
        }

        private PathString NormalizeVirtualPath(PathString path)
        {
            var url = path.Value;

            if (!string.IsNullOrEmpty(url) && (_options.LowercaseUrls || _options.AppendTrailingSlash))
            {
                var indexOfSeparator = url.IndexOfAny(new char[] { '?', '#' });
                var urlWithoutQueryString = url;
                var queryString = string.Empty;

                if (indexOfSeparator != -1)
                {
                    urlWithoutQueryString = url.Substring(0, indexOfSeparator);
                    queryString = url.Substring(indexOfSeparator);
                }

                if (_options.LowercaseUrls)
                {
                    urlWithoutQueryString = urlWithoutQueryString.ToLowerInvariant();
                }

                if (_options.AppendTrailingSlash && !urlWithoutQueryString.EndsWith("/"))
                {
                    urlWithoutQueryString += "/";
                }

                // queryString will contain the delimiter ? or # as the first character, so it's safe to append.
                url = urlWithoutQueryString + queryString;

                return new PathString(url);
            }

            return path;
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