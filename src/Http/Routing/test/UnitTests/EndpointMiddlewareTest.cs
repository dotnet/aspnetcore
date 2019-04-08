// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfAuthAttributesWereFound_ButAuthMiddlewareNotInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            });

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

            // Assert
            Assert.Equal("Endpoint Test contains authorization metadata, but a middleware was not found that supports authorization.", ex.Message);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_WorksIfAuthAttributesWereFound_AndAuthMiddlewareInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            });

            httpContext.Items[EndpointMiddleware.AuthorizationMiddlewareInvokedKey] = true;

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask);

            // Act & Assert
            await middleware.Invoke(httpContext);

            // If we got this far, we can sound the everything's OK alarm.
        }

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfCorsMetadataWasFound_ButCorsMiddlewareNotInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"),
            });

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

            // Assert
            Assert.Equal("Endpoint Test contains CORS metadata, but a middleware was not found that supports CORS.", ex.Message);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_WorksIfCorsMetadataWasFound_AndCorsMiddlewareInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            httpContext.Features.Set<IEndpointFeature>(new EndpointSelectorContext()
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"),
            });

            httpContext.Items[EndpointMiddleware.CorsMiddlewareInvokedKey] = true;

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask);

            // Act & Assert
            await middleware.Invoke(httpContext);

            // If we got this far, we can sound the everything's OK alarm.
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
