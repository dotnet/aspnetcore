// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class CompositeDispatcherValueConstraintTest
    {
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void CompositeRouteConstraint_Match_CallsMatchOnInnerConstraints(
            bool inner1Result,
            bool inner2Result,
            bool expected)
        {
            // Arrange
            var inner1 = MockConstraintWithResult(inner1Result);
            var inner2 = MockConstraintWithResult(inner2Result);

            // Act
            var constraint = new CompositeDispatcherValueConstraint(new[] { inner1.Object, inner2.Object });
            var actual = TestConstraint(constraint, null);

            // Assert
            Assert.Equal(expected, actual);
        }

        static Expression<Func<IDispatcherValueConstraint, bool>> ConstraintMatchMethodExpression =
            c => c.Match(
                It.IsAny<DispatcherValueConstraintContext>());

        private static Mock<IDispatcherValueConstraint> MockConstraintWithResult(bool result)
        {
            var mock = new Mock<IDispatcherValueConstraint>();
            mock.Setup(ConstraintMatchMethodExpression)
                .Returns(result)
                .Verifiable();
            return mock;
        }

        private static bool TestConstraint(IDispatcherValueConstraint constraint, object value, Action<IMatcher> routeConfig = null)
        {
            var httpContext = new DefaultHttpContext();
            var values = new DispatcherValueCollection() { { "fake", value } };
            var constraintPurpose = ConstraintPurpose.IncomingRequest;

            var dispatcherValueConstraintContext = new DispatcherValueConstraintContext(httpContext, values, constraintPurpose);

            return constraint.Match(dispatcherValueConstraintContext);
        }
    }
}
