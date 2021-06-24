// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class StatusCodeResultTests
    {
        [Fact]
        public void StatusCodeResult_ExecuteResultSetsResponseStatusCode()
        {
            // Arrange
            var result = new StatusCodeResult(StatusCodes.Status404NotFound);

            var httpContext = GetHttpContext();

            // Act
            result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }

        private static HttpContext GetHttpContext()
        {
            var services = CreateServices();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }
    }
}
