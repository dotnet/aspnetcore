// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class CreatedAtRouteResultTests
    {
        public static IEnumerable<object[]> CreatedAtRouteData
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
                            { "test", "case" },
                            { "sample", "route" }
                        })
                    };
            }
        }

        [Theory]
        [MemberData(nameof(CreatedAtRouteData))]
        public async Task CreatedAtRouteResult_ReturnsStatusCode_SetsLocationHeader(object values)
        {
            // Arrange
            var expectedUrl = "testAction";
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            var urlHelper = GetMockUrlHelper(expectedUrl);

            // Act
            var result = new CreatedAtRouteResult(routeName: null, routeValues: values, value: null);
            result.UrlHelper = urlHelper;
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(201, httpContext.Response.StatusCode);
            Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
        }

        [Fact]
        public async Task CreatedAtRouteResult_ThrowsOnNullUrl()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            var urlHelper = GetMockUrlHelper(returnValue: null);

            var result = new CreatedAtRouteResult(
                routeName: null,
                routeValues: new Dictionary<string, object>(),
                value: null);

            result.UrlHelper = urlHelper;

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await result.ExecuteResultAsync(actionContext),
            "No route matches the supplied values.");
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            return new ActionContext(httpContext,
                                    routeData,
                                    new ActionDescriptor());
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new Mock<HttpContext>();
            var realContext = new DefaultHttpContext();
            var request = realContext.Request;
            request.PathBase = new PathString("");
            var response = realContext.Response;
            response.Body = new MemoryStream();

            httpContext.Setup(o => o.Request)
                       .Returns(request);
            httpContext.Setup(o => o.Response)
                       .Returns(response);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(IOutputFormattersProvider)))
                       .Returns(new TestOutputFormatterProvider());

            return httpContext.Object;
        }

        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(o => o.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).Returns(returnValue);

            return urlHelper.Object;
        }

        private class TestOutputFormatterProvider : IOutputFormattersProvider
        {
            public IReadOnlyList<IOutputFormatter> OutputFormatters
            {
                get
                {
                    return new List<IOutputFormatter>()
                            {
                                new TextPlainFormatter(),
                                new JsonOutputFormatter()
                            };
                }
            }
        }
    }
}