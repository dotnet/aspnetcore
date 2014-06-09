// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

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
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _routeData = value;
            }
        }
    }
}
