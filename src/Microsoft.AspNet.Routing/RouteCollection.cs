﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollection : IRouteCollection
    {
        private readonly List<IRouter> _routes = new List<IRouter>();

        public IRouter this[int index]
        {
            get { return _routes[index]; }
        }

        public int Count
        {
            get { return _routes.Count; }
        }

        public IRouter DefaultHandler { get; set; }

        public void Add(IRouter router)
        {
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

        public virtual string BindPath(BindPathContext context)
        {
            for (var i = 0; i < Count; i++)
            {
                var route = this[i];

                var path = route.BindPath(context);
                if (path != null)
                {
                    return path;
                }
            }

            return null;
        }
    }
}
