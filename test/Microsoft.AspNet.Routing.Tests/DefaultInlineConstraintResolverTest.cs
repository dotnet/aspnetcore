// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class DefaultInlineConstraintResolverTest
    {
        [Fact]
        public void ResolveConstraint_IntConstraint_ResolvesCorrectly()
        {
            // Arrange
            var routeOptions = new RouteOptions();
            var constraintResolver = GetInlineConstraintResolver(routeOptions);

            // Act
            var constraint = constraintResolver.ResolveConstraint("int");

            // Assert
            Assert.IsType<IntRouteConstraint>(constraint);
        }

        [Fact]
        public void ResolveConstraint_IntConstraintWithArgument_Throws()
        {
            // Arrange
            var routeOptions = new RouteOptions();
            var constraintResolver = GetInlineConstraintResolver(routeOptions);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => constraintResolver.ResolveConstraint("int(5)"));
            Assert.Equal("Could not find a constructor for constraint type 'IntRouteConstraint'"+
                         " with the following number of parameters: 1.",
                         ex.Message);
        }

        [Fact]
        public void ResolveConstraint_SupportsCustomConstraints()
        {
            // Arrange
            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap.Add("custom", typeof(CustomRouteConstraint));
            var resolver = GetInlineConstraintResolver(routeOptions);

            // Act
            var constraint = resolver.ResolveConstraint("custom(argument)");

            // Assert
            Assert.IsType<CustomRouteConstraint>(constraint);
        }

        [Fact]
        public void ResolveConstraint_CustomConstraintThatDoesNotImplementIRouteConstraint_Throws()
        {
            // Arrange
            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap.Add("custom", typeof(string));
            var resolver = GetInlineConstraintResolver(routeOptions);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => resolver.ResolveConstraint("custom"));
            Assert.Equal("The constraint type 'System.String' which is mapped to constraint key 'custom'"+
                         " must implement the 'IRouteConstraint' interface.", 
                         ex.Message);
        }

        private IInlineConstraintResolver GetInlineConstraintResolver(RouteOptions routeOptions)
        {
            var optionsAccessor = new Mock<IOptionsAccessor<RouteOptions>>();
            optionsAccessor.SetupGet(o => o.Options).Returns(routeOptions);
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(o => o.GetService(It.Is<Type>(type => type == typeof(ITypeActivator))))
                           .Returns(new TypeActivator());
            return new DefaultInlineConstraintResolver(serviceProvider.Object, optionsAccessor.Object);
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
#endif
