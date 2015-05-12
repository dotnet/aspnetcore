// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class RedirectToRouteResultTest
    {
        [Theory]
        [MemberData(nameof(RedirectToRouteData))]
        public async void RedirectToRoute_Execute_PassesCorrectValuesToRedirect(object values)
        {
            // Arrange
            var expectedUrl = "SampleAction";
            var expectedPermanentFlag = false;
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.Setup(o => o.Response).Returns(httpResponse.Object);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(ITempDataDictionary)))
                .Returns(Mock.Of<ITempDataDictionary>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  new RouteData(),
                                                  new ActionDescriptor());

            var urlHelper = GetMockUrlHelper(expectedUrl);
            var result = new RedirectToRouteResult(null, TypeHelper.ObjectToDictionary(values))
            {
                UrlHelper = urlHelper,
            };

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
                                                  new RouteData(),
                                                  new ActionDescriptor());

            var urlHelper = GetMockUrlHelper(returnValue: null);
            var result = new RedirectToRouteResult(null, new Dictionary<string, object>())
            {
                UrlHelper = urlHelper,
            };

            // Act & Assert
            ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await result.ExecuteResultAsync(actionContext);
                },
                "No route matches the supplied values.");
        }

        [Fact]
        public void RedirectToRoute_Execute_Calls_TempDataKeep()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>();
            tempData.Setup(t => t.Keep()).Verifiable();

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(o => o.Response).Returns(new Mock<HttpResponse>().Object);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(ITempDataDictionary))).Returns(tempData.Object);
            var actionContext = new ActionContext(httpContext.Object,
                                                  new RouteData(),
                                                  new ActionDescriptor());

            var result = new RedirectToRouteResult("SampleRoute", null)
            {
                UrlHelper = GetMockUrlHelper("SampleRoute")
            };

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            tempData.Verify(t => t.Keep(), Times.Once());
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesRouteName_ToGenerateLocationHeader()
        {
            // Arrange
            var routeName = "orders_api";
            var locationUrl = "/api/orders/10";
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(uh => uh.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns(locationUrl)
                .Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IUrlHelper)))
                .Returns(urlHelper.Object);
            serviceProvider.Setup(sp => sp.GetService(typeof(ITempDataDictionary)))
                .Returns(new Mock<ITempDataDictionary>().Object);
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider.Object;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new RedirectToRouteResult(routeName, new { id = 10 });

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            urlHelper.Verify(uh => uh.RouteUrl(
                It.Is<UrlRouteContext>(routeContext => string.Equals(routeName, routeContext.RouteName))));
            Assert.True(httpContext.Response.Headers.ContainsKey("Location"), "Location header not found");
            Assert.Equal(locationUrl, httpContext.Response.Headers["Location"]);
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
            urlHelper.Setup(o => o.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(returnValue);
            return urlHelper.Object;
        }
    }
}
