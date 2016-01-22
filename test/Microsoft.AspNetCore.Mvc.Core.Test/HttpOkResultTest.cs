// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpOkResultTest
    {
        [Fact]
        public void HttpOkResult_InitializesStatusCode()
        {
            // Arrange & Act
            var result = new HttpOkResult();

            // Assert
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async Task HttpOkResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices().BuildServiceProvider();

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new HttpOkResult();

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.HttpContext.Response.StatusCode);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }
    }
}
