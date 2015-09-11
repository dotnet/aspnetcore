// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteConstraint
    {
        bool Match(HttpContext httpContext,
                   IRouter route,
                   string routeKey,
                   IDictionary<string, object> values,
                   RouteDirection routeDirection);
    }
}
