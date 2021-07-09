// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class AcceptedResultTests
    {
        [Fact]
        public async Task ExecuteResultAsync_FormatsData()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var stream = new MemoryStream();
            httpContext.Response.Body = stream;
            // Act
            var result = new AcceptedResult("my-location", value: "Hello world");
            await result.ExecuteAsync(httpContext);

            // Assert
            var response = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("\"Hello world\"", response);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
        {
            // Arrange
            var expectedUrl = "testAction";
            var httpContext = GetHttpContext();

            // Act
            var result = new AcceptedResult(expectedUrl, value: "some-value");
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
            Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();
            return httpContext;
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            return services.BuildServiceProvider();
        }
    }
}
