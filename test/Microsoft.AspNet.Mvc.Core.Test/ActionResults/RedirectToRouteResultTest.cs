// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class RedirectToRouteResultTest
    {
        [Theory]
        [MemberData("RedirectToRouteData")]
        public async void RedirectToRoute_Execute_PassesCorrectValuesToRedirect(object values)
        {
            // Arrange
            var expectedUrl = "SampleAction";
            var expectedPermanentFlag = false;
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.Setup(o => o.Response).Returns(httpResponse.Object);

            var actionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<IRouter>(),
                                                  new Dictionary<string, object>(),
                                                  new ActionDescriptor());
            IUrlHelper urlHelper = GetMockUrlHelper(expectedUrl);
            RedirectToRouteResult result = new RedirectToRouteResult(urlHelper,
                                                                     null,
                                                                     TypeHelper.ObjectToDictionary(values));

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            // Thus we verify that the Url returned by UrlHelper is passed properly to
            // Redirect method and that the method is called exactly once.
            httpResponse.Verify(r => r.Redirect(expectedUrl, expectedPermanentFlag), Times.Exactly(1));
        }

        [Fact]
        public void RedirectToRoute_Execute_ThrowsOnNullUrl()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(o => o.Response).Returns(new Mock<HttpResponse>().Object);
            var actionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<IRouter>(),
                                                  new Dictionary<string, object>(),
                                                  new ActionDescriptor());

            IUrlHelper urlHelper = GetMockUrlHelper(returnValue: null);
            RedirectToRouteResult result = new RedirectToRouteResult(urlHelper, 
                                                                     null,
                                                                     new Dictionary<string, object>());

            // Act & Assert
            ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await result.ExecuteResultAsync(actionContext);
                },
                "No route matches the supplied values.");
        }

        public static IEnumerable<object[]> RedirectToRouteData
        {
            get
            {
                yield return new object[] { null };
                yield return
                    new object[] {
                        new Dictionary<string, string>() { { "hello", "world" } }
                    };
                yield return
                    new object[] {
                        new RouteValueDictionary(new Dictionary<string, string>() { 
                                                        { "test", "case" }, { "sample", "route" } })
                    };
            }
        }

        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(o => o.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).Returns(returnValue);
            return urlHelper.Object;
        }
    }
}
