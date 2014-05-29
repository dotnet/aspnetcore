// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollection : IRouteCollection
    {
        private readonly List<IRouter> _routes = new List<IRouter>();
        private readonly List<IRouter> _unnamedRoutes = new List<IRouter>();
        private readonly Dictionary<string, INamedRouter> _namedRoutes = 
                                    new Dictionary<string, INamedRouter>(StringComparer.OrdinalIgnoreCase);

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

                await route.RouteAsync(context);
                if (context.IsHandled)
                {
                    return;
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
    }
}