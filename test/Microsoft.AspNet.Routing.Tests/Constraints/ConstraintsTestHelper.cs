// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Moq;

namespace Microsoft.AspNet.Routing.Tests
{
    public class ConstraintsTestHelper
    {
        public static bool TestConstraint(IRouteConstraint constraint, object value, Action<IRouter> routeConfig = null)
        {
            var context = new Mock<HttpContext>();

            var route = new RouteCollection();

            if (routeConfig != null)
            {
                routeConfig(route);
            }

            var parameterName = "fake";
            var values = new Dictionary<string, object>() { { parameterName, value } };
            var routeDirection = RouteDirection.IncomingRequest;
            return constraint.Match(context.Object, route, parameterName, values, routeDirection);
        }
    }
}

#endif
