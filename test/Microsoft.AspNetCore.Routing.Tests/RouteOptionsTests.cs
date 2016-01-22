// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class RouteOptionsTests
    {
        [Fact]
        public void ConfigureRouting_ConfiguresOptionsProperly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();

            // Act
            services.AddRouting(options => options.ConstraintMap.Add("foo", typeof(TestRouteConstraint)));
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
            Assert.Equal("TestRouteConstraint", accessor.Value.ConstraintMap["foo"].Name);
        }

        private class TestRouteConstraint : IRouteConstraint
        {
            public TestRouteConstraint(string pattern)
            {
                Pattern = pattern;
            }

            public string Pattern { get; private set; }
            public bool Match(
                HttpContext httpContext,
                IRouter route,
                string routeKey,
                RouteValueDictionary values,
                RouteDirection routeDirection)
            {
                throw new NotImplementedException();
            }
        }
    }
}
