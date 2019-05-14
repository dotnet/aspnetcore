// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointMiddlewareTest
    {
        [Fact]
        public async Task Invoke_NoFeature_NoOps()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();

            RequestDelegate next = (c) =>
            {
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next);

            // Act
            await middleware.Invoke(httpContext);

            // Assert - does not throw
        }

        [Fact]
        public async Task Invoke_NoEndpoint_NoOps()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = null,
            });

            RequestDelegate next = (c) =>
            {
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next);

            // Act
            await middleware.Invoke(httpContext);

            // Assert - does not throw
        }

        [Fact]
        public async Task Invoke_WithEndpoint_InvokesDelegate()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();

            var invoked = false;
            RequestDelegate endpointFunc = (c) =>
            {
                invoked = true;
                return Task.CompletedTask;
            };

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = new Endpoint(endpointFunc, EndpointMetadataCollection.Empty, "Test"),
            });

            RequestDelegate next = (c) =>
            {
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(invoked);
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
