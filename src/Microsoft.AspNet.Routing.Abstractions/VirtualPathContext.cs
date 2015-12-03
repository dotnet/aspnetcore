// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing
{
    public class VirtualPathContext
    {
        public VirtualPathContext(
            HttpContext httpContext,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values)
            : this(httpContext, ambientValues, values, null)
        {
        }

        public VirtualPathContext(
            HttpContext context,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values,
            string routeName)
        {
            Context = context;
            AmbientValues = ambientValues;
            Values = values;
            RouteName = routeName;
        }

        public string RouteName { get; }

        public IDictionary<string, object> ProvidedValues { get; set; }

        public RouteValueDictionary AmbientValues { get; }

        public HttpContext Context { get; }

        public bool IsBound { get; set; }

        public RouteValueDictionary Values { get; }
    }
}
