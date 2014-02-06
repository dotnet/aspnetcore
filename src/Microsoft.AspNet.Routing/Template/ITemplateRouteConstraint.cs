// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing.Template
{
    public interface ITemplateRouteConstraint
    {
        bool Match(HttpContext context, IRoute route, string parameterName, IDictionary<string, object> values, RouteDirection routeDirection);
    }
}
