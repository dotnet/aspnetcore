// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    internal class CapturingConstraint : IRouteConstraint
    {
        public IDictionary<string, object> Values { get; private set; }

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            Values = new RouteValueDictionary(values);
            return true;
        }
    }
}
