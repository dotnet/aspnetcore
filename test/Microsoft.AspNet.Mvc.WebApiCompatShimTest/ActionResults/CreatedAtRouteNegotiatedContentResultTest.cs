// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class CreatedAtRouteNegotiatedContentResultTest
    {
        [Fact]
        public async Task CreatedAtRouteNegotiatedContentResult_SetsStatusCode()
        {
            // Arrange
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelper
                .Setup(u => u.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("http://contoso.com/api/Products/5");

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices(urlHelper.Object);

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedAtRouteNegotiatedContentResult<Product>(
                "api_route", 
                new RouteValueDictionary(new { controller = "Products", id = 5 }), 
                new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(201, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task CreatedAtRouteNegotiatedContentResult_SetsLocation()
        {
            // Arrange
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelper
                .Setup(u => u.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("http://contoso.com/api/Products/5");

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices(urlHelper.Object);

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedAtRouteNegotiatedContentResult<Product>(
                "api_route",
                new RouteValueDictionary(new { controller = "Products", id = 5 }),
                new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal("http://contoso.com/api/Products/5", httpContext.Response.Headers["Location"]);
        }

        [Fact]
        public async Task CreatedAtRouteNegotiatedContentResult_Fails()
        {
            // Arrange
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelper
                .Setup(u => u.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string)null);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices(urlHelper.Object);

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedAtRouteNegotiatedContentResult<Product>(
                "api_route",
                new RouteValueDictionary(new { controller = "Products", id = 5 }),
                new Product());

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await result.ExecuteResultAsync(context));

            // Assert
            Assert.Equal("Failed to generate a URL using route 'api_route'.", ex.Message);
        }

        private IServiceProvider CreateServices(IUrlHelper urlHelper)
        {
            var services = new Mock<IServiceProvider>(MockBehavior.Strict);

            services
                .Setup(s => s.GetService(typeof(IUrlHelper)))
                .Returns(urlHelper);

            var formatters = new Mock<IOutputFormattersProvider>(MockBehavior.Strict);
            formatters
                .SetupGet(f => f.OutputFormatters)
                .Returns(new List<IOutputFormatter>() { new JsonOutputFormatter(), });

            services
                .Setup(s => s.GetService(typeof(IOutputFormattersProvider)))
                .Returns(formatters.Object);

            return services.Object;
        }

        private class Product
        {
            public int Id { get; set; }

            public string Name { get; set; }
        };
    }
}
#endif