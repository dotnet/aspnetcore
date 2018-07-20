// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class RouteConstraintMatchProcessorTest
    {
        [Fact]
        public void MatchInbound_CallsRouteConstraint()
        {
            // Arrange
            var constraint = new Mock<IRouteConstraint>();
            constraint
                .Setup(c => c.Match(
                    It.IsAny<HttpContext>(),
                    NullRouter.Instance,
                    "test",
                    It.IsAny<RouteValueDictionary>(),
                    RouteDirection.IncomingRequest))
                    .Returns(true)
                    .Verifiable();

            var matchProcessor = new RouteConstraintMatchProcessor("test", constraint.Object);

            // Act
            var result = matchProcessor.ProcessInbound(new DefaultHttpContext(), new RouteValueDictionary());

            // Assert
            Assert.True(result);
            constraint.Verify();
        }

        [Fact]
        public void MatchOutput_CallsRouteConstraint()
        {
            // Arrange
            var constraint = new Mock<IRouteConstraint>();
            constraint
                .Setup(c => c.Match(
                    It.IsAny<HttpContext>(),
                    NullRouter.Instance,
                    "test",
                    It.IsAny<RouteValueDictionary>(),
                    RouteDirection.UrlGeneration))
                    .Returns(true)
                    .Verifiable();

            var matchProcessor = new RouteConstraintMatchProcessor("test", constraint.Object);

            // Act
            var result = matchProcessor.ProcessOutbound(new DefaultHttpContext(), new RouteValueDictionary());

            // Assert
            Assert.True(result);
            constraint.Verify();
        }
    }
}
