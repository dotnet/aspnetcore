// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RouteOptionsTests
    {
        [Fact]
        public void ConstraintMap_SettingNullValue_Throws()
        {
            // Arrange
            var options = new RouteOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.ConstraintMap = null);
            Assert.Equal("The 'ConstraintMap' property of 'Microsoft.AspNet.Routing.RouteOptions' must not be null." +
                         Environment.NewLine + "Parameter name: value", ex.Message);
        }

        [Fact]
        public void ConfigureRouteOptions_ConfiguresOptionsProperly()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();

            // Act
            services.ConfigureRouteOptions(options => options.ConstraintMap.Add("foo", typeof(TestRouteConstraint)));
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
            Assert.Equal("TestRouteConstraint", accessor.Options.ConstraintMap["foo"].Name);
        }

        private class TestRouteConstraint : IRouteConstraint
        {
            public TestRouteConstraint(string pattern)
            {
                Pattern = pattern;
            }

            public string Pattern { get; private set; }
            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey,
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                throw new NotImplementedException();
            }
        }
    }
}
