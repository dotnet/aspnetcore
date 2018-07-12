// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointMiddlewareTest
    {
        [Fact]
        public async Task Invoke_NoFeature_ThrowFriendlyErrorMessage()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next);

            // Act
            var invokeTask = middleware.Invoke(httpContext);

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await invokeTask);

            Assert.Equal("Unable to execute an endpoint because the dispatcher was not run. Ensure dispatcher middleware is registered.", ex.Message);
        }

        private class ServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
