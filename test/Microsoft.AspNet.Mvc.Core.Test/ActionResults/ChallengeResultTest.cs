// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.ActionResults
{
    public class ChallengeResultTest
    {
        [Fact]
        public void ChallengeResult_Execute()
        {
            // Arrange
            var result = new ChallengeResult(new string[] { }, null);
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.Setup(o => o.Response).Returns(httpResponse.Object);

            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            httpResponse.Verify(c => c.Challenge(null, (IEnumerable<string>)new string[] { }), Times.Exactly(1));
        }
    }
}