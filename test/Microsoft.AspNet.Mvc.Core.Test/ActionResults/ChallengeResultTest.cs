// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ActionResults
{
    public class ChallengeResultTest
    {
        [Fact]
        public async Task ChallengeResult_Execute()
        {
            // Arrange
            var result = new ChallengeResult("", null);
            var httpContext = new Mock<HttpContext>();
            var auth = new Mock<AuthenticationManager>();
            httpContext.Setup(o => o.Authentication).Returns(auth.Object);

            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync("", null), Times.Exactly(1));
        }

        [Fact]
        public async Task ChallengeResult_ExecuteNoSchemes()
        {
            // Arrange
            var result = new ChallengeResult(new string[] { }, null);
            var httpContext = new Mock<HttpContext>();
            var auth = new Mock<AuthenticationManager>();
            httpContext.Setup(o => o.Authentication).Returns(auth.Object);

            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync((AuthenticationProperties)null), Times.Exactly(1));
        }
    }
}