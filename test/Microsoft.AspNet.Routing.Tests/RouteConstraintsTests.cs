// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RouteConstraintsTests
    {
        [Theory]
        [InlineData(42, true)]
        [InlineData("42", true)]
        [InlineData(3.14, false)]
        [InlineData("43.567", false)]
        [InlineData("42a", false)]
        public void IntRouteConstraint_Match_AppliesConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new IntRouteConstraint();

            // Act
            var actual = TestValue(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
        
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void CompoundRouteConstraint_Match_CallsMatchOnInnerConstraints(bool inner1Result,
                                                                               bool inner2Result,
                                                                               bool expected)
        {
            // Arrange
            var inner1 = MockConstraintWithResult(inner1Result);
            var inner2 = MockConstraintWithResult(inner2Result);

            // Act
            var constraint = new CompositeRouteConstraint(new[] { inner1.Object, inner2.Object });
            var actual = TestValue(constraint, null);

            // Assert
            Assert.Equal(expected, actual);
        }

        static Expression<Func<IRouteConstraint, bool>> ConstraintMatchMethodExpression = 
            c => c.Match(It.IsAny<HttpContext>(),
                         It.IsAny<IRouter>(),
                         It.IsAny<string>(),
                         It.IsAny<Dictionary<string, object>>(),
                         It.IsAny<RouteDirection>());

        private static Mock<IRouteConstraint> MockConstraintWithResult(bool result)
        {
            var mock = new Mock<IRouteConstraint>();
            mock.Setup(ConstraintMatchMethodExpression)
                .Returns(result)
                .Verifiable();
            return mock;
        }

        private static void AssertMatchWasCalled(Mock<IRouteConstraint> mock, Times times)
        {
            mock.Verify(ConstraintMatchMethodExpression, times);
        }

        private static bool TestValue(IRouteConstraint constraint, object value, Action<IRouter> routeConfig = null)
        {
            var context = new Mock<HttpContext>();

            IRouter route = new RouteCollection();

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