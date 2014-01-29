﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class DefaultRouteCollection : IRouteCollection
    {
        private readonly List<IRoute> _routes = new List<IRoute>();

        public IRoute this[int index]
        {
            get { return _routes[index]; }
        }

        public int Count
        {
            get { return _routes.Count; }
        }

        public void Add(IRoute route)
        {
            _routes.Add(route);
        }
    }
}
