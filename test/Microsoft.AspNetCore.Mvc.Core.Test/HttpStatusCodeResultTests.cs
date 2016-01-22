// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    public class HttpStatusCodeResultTests
    {
        [Fact]
        public void HttpStatusCodeResult_ExecuteResultSetsResponseStatusCode()
        {
            // Arrange
            var result = new HttpStatusCodeResult(StatusCodes.Status404NotFound);

            var httpContext = GetHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            var context = new ActionContext(httpContext, routeData, actionDescriptor);

            // Act
            result.ExecuteResult(context);

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