// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class DefaultInlineConstraintResolverTest
    {
        [Fact]
        public void ResolveConstraint_IntConstraint_ResolvesCorrectly()
        {
            // Arrange & Act
            var constraint = new DefaultInlineConstraintResolver().ResolveConstraint("int");

            // Assert
            Assert.IsType<IntRouteConstraint>(constraint);
        }

        [Fact]
        public void ResolveConstraint_IntConstraintWithArgument_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => new DefaultInlineConstraintResolver().ResolveConstraint("int(5)"));
            Assert.Equal("Could not find a constructor for constraint type 'IntRouteConstraint'"+
                         " with the following number of parameters: 1.",
                         ex.Message);
        }

        [Fact]
        public void ResolveConstraint_SupportsCustomConstraints()
        {
            // Arrange
            var resolver = new DefaultInlineConstraintResolver();
            resolver.ConstraintMap.Add("custom", typeof(CustomRouteConstraint));

            // Act
            var constraint = resolver.ResolveConstraint("custom(argument)");

            // Assert
            Assert.IsType<CustomRouteConstraint>(constraint);
        }

        [Fact]
        public void ResolveConstraint_CustomConstraintThatDoesNotImplementIRouteConstraint_Throws()
        {
            // Arrange
            var resolver = new DefaultInlineConstraintResolver();
            resolver.ConstraintMap.Add("custom", typeof(string));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => resolver.ResolveConstraint("custom"));
            Assert.Equal("The constraint type 'System.String' which is mapped to constraint key 'custom'"+
                         " must implement the 'IRouteConstraint' interface.", 
                         ex.Message);
        }

        private class CustomRouteConstraint : IRouteConstraint
        {
            public CustomRouteConstraint(string pattern)
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
                return true;
            }
        }
    }
}
