// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        public void Add(IRouter router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

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

                    if (context.Handler != null)
                    {
                        break;
                    }
                }
                finally
                {
                    if (context.Handler == null)
                    {
                        context.RouteData = oldRouteData;
                    }
                }
            }
        }

        public virtual VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            EnsureOptions(context.HttpContext);

            if (!string.IsNullOrEmpty(context.RouteName))
            {
                VirtualPathData namedRoutePathData = null;
                INamedRouter matchedNamedRoute;
                if (_namedRoutes.TryGetValue(context.RouteName, out matchedNamedRoute))
                {
                    namedRoutePathData = matchedNamedRoute.GetVirtualPath(context);
                }

                var pathData = GetVirtualPath(context, _unnamedRoutes);

                // If the named route and one of the unnamed routes also matches, then we have an ambiguity.
                if (namedRoutePathData != null && pathData != null)
                {
                    var message = Resources.FormatNamedRoutes_AmbiguousRoutesFound(context.RouteName);
                    throw new InvalidOperationException(message);
                }

                return NormalizeVirtualPath(namedRoutePathData ?? pathData);
            }
            else
            {
                return NormalizeVirtualPath(GetVirtualPath(context, _routes));
            }
        }

        private VirtualPathData GetVirtualPath(VirtualPathContext context, List<IRouter> routes)
        {
            for (var i = 0; i < routes.Count; i++)
            {
                var route = routes[i];

                var pathData = route.GetVirtualPath(context);
                if (pathData != null)
                {
                    return pathData;
                }
            }

            return null;
        }

        private VirtualPathData NormalizeVirtualPath(VirtualPathData pathData)
        {
            if (pathData == null)
            {
                return pathData;
            }

            var url = pathData.VirtualPath;

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

                return new VirtualPathData(pathData.Router, url, pathData.DataTokens);
            }

            return pathData;
        }

        private void EnsureOptions(HttpContext context)
        {
            if (_options == null)
            {
                _options = context.RequestServices.GetRequiredService<IOptions<RouteOptions>>().Value;
            }
        }
    }
}