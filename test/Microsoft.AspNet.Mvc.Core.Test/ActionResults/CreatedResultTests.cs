// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class CreatedResultTests
    {
        [Fact]
        public void CreatedResult_SetsLocation()
        {
            // Arrange
            var location = "http://test/location";

            // Act
            var result = new CreatedResult(location, "testInput");

            // Assert
            Assert.Same(location, result.Location);
        }

        [Fact]
        public async Task CreatedResult_ReturnsStatusCode_SetsLocationHeader()
        {
            // Arrange
            var location = "/test/";
            var response = GetMockedHttpResponseObject();
            var httpContext = GetHttpContext(response);
            var actionContext = GetActionContext(httpContext);
            var result = new CreatedResult(location, "testInput");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(201, response.StatusCode);
            Assert.Equal(location, response.Headers["Location"]);
        }

        private static HttpResponse GetMockedHttpResponseObject()
        {
            var stream = new MemoryStream();
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupProperty(o => o.StatusCode);
            httpResponse.Setup(o => o.Headers).Returns(
                new HeaderDictionary(new Dictionary<string, string[]>()));
            httpResponse.SetupGet(o => o.Body).Returns(stream);
            return httpResponse.Object;
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            return new ActionContext(httpContext,
                                    routeData,
                                    new ActionDescriptor());
        }

        private static HttpContext GetHttpContext(HttpResponse response)
        {
            var httpContext = new Mock<HttpContext>();

            httpContext.Setup(o => o.Response)
                       .Returns(response);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(IOutputFormattersProvider)))
                       .Returns(new TestOutputFormatterProvider());
            httpContext.Setup(o => o.Request.PathBase)
                       .Returns(new PathString(""));

            return httpContext.Object;
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