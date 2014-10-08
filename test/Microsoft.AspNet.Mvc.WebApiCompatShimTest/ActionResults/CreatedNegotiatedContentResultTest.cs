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
    public class CreatedNegotiatedContentResultTest
    {
        [Fact]
        public async Task CreatedNegotiatedContentResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var uri = new Uri("http://contoso.com");

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedNegotiatedContentResult<Product>(uri, new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(201, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task CreatedNegotiatedContentResult_SetsLocation_Uri()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var uri = new Uri("http://contoso.com");

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedNegotiatedContentResult<Product>(uri, new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal("http://contoso.com/", httpContext.Response.Headers["Location"]);
        }

        [Theory]
        [InlineData("http://contoso.com/Api/Products")]
        [InlineData("/Api/Products")]
        [InlineData("Products")]
        public async Task CreatedNegotiatedContentResult_SetsLocation_String(string uri)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new CreatedNegotiatedContentResult<Product>(
                new Uri(uri, UriKind.RelativeOrAbsolute),
                new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(uri, httpContext.Response.Headers["Location"]);
        }

        private IServiceProvider CreateServices()
        {
            var services = new Mock<IServiceProvider>(MockBehavior.Strict);

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