// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointMiddlewareTest
    {
        private readonly IOptions<RouteOptions> RouteOptions = Options.Create(new RouteOptions());

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

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

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

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = null,
            };

            RequestDelegate next = (c) =>
            {
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

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

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(endpointFunc, EndpointMetadataCollection.Empty, "Test"),
            };

            RequestDelegate next = (c) =>
            {
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfAuthAttributesWereFound_ButAuthMiddlewareNotInvoked()
        {
            // Arrange
            var expected = "Endpoint Test contains authorization metadata, but a middleware was not found that supports authorization." +
                Environment.NewLine +
                "Configure your application startup by adding app.UseAuthorization() inside the call to Configure(..) in the application startup code.";
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, RouteOptions);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_WorksIfAuthAttributesWereFound_AndAuthMiddlewareInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            };

            httpContext.Items[EndpointMiddleware.AuthorizationMiddlewareInvokedKey] = true;

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, RouteOptions);

            // Act & Assert
            await middleware.Invoke(httpContext);

            // If we got this far, we can sound the everything's OK alarm.
        }

        [Fact]
        public async Task Invoke_WithEndpoint_DoesNotThrowIfUnhandledAuthAttributesWereFound_ButSuppressedViaOptions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            };

            var routeOptions = Options.Create(new RouteOptions { SuppressCheckForUnhandledSecurityMetadata = true });
            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, routeOptions);

            // Act & Assert
            await middleware.Invoke(httpContext);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfCorsMetadataWasFound_ButCorsMiddlewareNotInvoked()
        {
            // Arrange
            var expected = "Endpoint Test contains CORS metadata, but a middleware was not found that supports CORS." +
                Environment.NewLine +
                "Configure your application startup by adding app.UseCors() inside the call to Configure(..) in the application startup code.";
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"),
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, RouteOptions);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_WorksIfCorsMetadataWasFound_AndCorsMiddlewareInvoked()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"),
            };

            httpContext.Items[EndpointMiddleware.CorsMiddlewareInvokedKey] = true;

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, RouteOptions);

            // Act & Assert
            await middleware.Invoke(httpContext);

            // If we got this far, we can sound the everything's OK alarm.
        }

        [Fact]
        public async Task Invoke_WithEndpoint_DoesNotThrowIfUnhandledCorsAttributesWereFound_ButSuppressedViaOptions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"),
            };

            var routeOptions = Options.Create(new RouteOptions { SuppressCheckForUnhandledSecurityMetadata = true });
            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, _ => Task.CompletedTask, routeOptions);

            // Act & Assert
            await middleware.Invoke(httpContext);
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
