// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

﻿using System;
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

        public IRouter DefaultHandler { get; set; }

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