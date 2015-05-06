// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Routing
{
    public class RouteContext
    {
        private RouteData _routeData;

        public RouteContext(HttpContext httpContext)
        {
            HttpContext = httpContext;

            RouteData = new RouteData();
        }

        public HttpContext HttpContext { get; private set; }

        public bool IsHandled { get; set; }

        public RouteData RouteData
        {
            get
            {
                return _routeData;
            }
            [param: NotNull]
            set
            {
                _routeData = value;
            }
        }
    }
}
