// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
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
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            var result = new CreatedResult(location, "testInput");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
            Assert.Equal(location, httpContext.Response.Headers["Location"]);
        }

        [Fact]
        public async Task CreatedResult_OverwritesLocationHeader()
        {
            // Arrange
            var location = "/test/";
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            httpContext.Response.Headers["Location"] = "/different/location/";
            var result = new CreatedResult(location, "testInput");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
            Assert.Equal(location, httpContext.Response.Headers["Location"]);
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
            var optionsAccessor = new MockMvcOptionsAccessor();
            optionsAccessor.Options.OutputFormatters.Add(new StringOutputFormatter());
            optionsAccessor.Options.OutputFormatters.Add(new JsonOutputFormatter());
            httpContext
                .Setup(p => p.RequestServices.GetService(typeof(IOptions<MvcOptions>)))
                .Returns(optionsAccessor);
            httpContext
                .Setup(p => p.RequestServices.GetService(typeof(ILogger<ObjectResult>)))
                .Returns(new Mock<ILogger<ObjectResult>>().Object);

            var actionBindingContext = new ActionBindingContext()
            {
                OutputFormatters = optionsAccessor.Options.OutputFormatters
            };
            httpContext
                .Setup(o => o.RequestServices.GetService(typeof(IActionBindingContextAccessor)))
                .Returns(new ActionBindingContextAccessor() { ActionBindingContext = actionBindingContext });

            return httpContext.Object;
        }
    }
}