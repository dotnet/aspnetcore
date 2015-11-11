// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpNotFoundObjectResultTest
    {
        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCode()
        {
            // Arrange & act
            var notFound = new HttpNotFoundObjectResult(null);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCodeAndResponseContent()
        {
            // Arrange & act
            var notFound = new HttpNotFoundObjectResult("Test Content");

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            Assert.Equal("Test Content", notFound.Value);
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_ExecuteSuccessful()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext,
            };

            var result = new HttpNotFoundObjectResult("Test Content");
            
            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.PathBase = new PathString("");
            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = CreateServices();
            return httpContext;
        }

        private static IServiceProvider CreateServices()
        {
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter());

            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }
    }
}