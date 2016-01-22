// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class RouteDataActionConstraintTest
    {
        [Fact]
        public void RouteDataActionConstraint_DenyKeyByPassingEmptyString()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", string.Empty);

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.DenyKey);
            Assert.Equal(routeDataConstraint.RouteValue, string.Empty);
        }

        [Fact]
        public void RouteDataActionConstraint_DenyKeyByPassingNull()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", null);

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.DenyKey);
            Assert.Equal(routeDataConstraint.RouteValue, string.Empty);
        }

        [Fact]
        public void RouteDataActionConstraint_RequireKeyByPassingNonEmpty()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", "value");

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.RequireKey);
            Assert.Equal(routeDataConstraint.RouteValue, "value");
        }
    }
}