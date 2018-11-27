// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class HttpNotFoundObjectResultTest
    {
        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCode()
        {
            // Arrange & act
            var notFound = new NotFoundObjectResult(null);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCodeAndResponseContent()
        {
            // Arrange & act
            var notFound = new NotFoundObjectResult("Test Content");

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

            var result = new NotFoundObjectResult("Test Content");

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
            var options = Options.Create(new MvcOptions());
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter(
                new JsonSerializerSettings(),
                ArrayPool<char>.Shared));

            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
                new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }
    }
}