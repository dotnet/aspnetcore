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

            var calledNext = false;
            RequestDelegate next = (c) =>
            {
                calledNext = true;
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledNext);
        }

        [Fact]
        public async Task Invoke_NoEndpoint_NoOps()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();
            httpContext.SetEndpoint(null);

            var calledNext = false;
            RequestDelegate next = (c) =>
            {
                calledNext = true;
                return Task.CompletedTask;
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledNext);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_InvokesDelegate()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceProvider();

            var calledEndpoint = false;
            RequestDelegate endpointFunc = (c) =>
            {
                calledEndpoint = true;
                return Task.CompletedTask;
            };

            httpContext.SetEndpoint(new Endpoint(endpointFunc, EndpointMetadataCollection.Empty, "Test"));

            RequestDelegate next = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledEndpoint);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfAuthAttributesWereFound_ButAuthMiddlewareNotInvoked()
        {
            // Arrange
            var expected = "Endpoint Test contains authorization metadata, but a middleware was not found that supports authorization." +
                Environment.NewLine +
                "Configure your application startup by adding app.UseAuthorization() inside the call to Configure(..) in the application startup code. " +
                "The call to app.UseAuthorization() must appear between app.UseRouting() and app.UseEndpoints(...).";
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            RequestDelegate throwIfCalled = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            httpContext.SetEndpoint(new Endpoint(throwIfCalled, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"));

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, throwIfCalled, RouteOptions);

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

            var calledEndpoint = false;
            RequestDelegate endpointFunc = (c) =>
            {
                calledEndpoint = true;
                return Task.CompletedTask;
            };

            httpContext.SetEndpoint(new Endpoint(endpointFunc, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"));

            httpContext.Items[EndpointMiddleware.AuthorizationMiddlewareInvokedKey] = true;

            RequestDelegate next = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledEndpoint);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_DoesNotThrowIfUnhandledAuthAttributesWereFound_ButSuppressedViaOptions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            var calledEndpoint = false;
            RequestDelegate endpointFunc = (c) =>
            {
                calledEndpoint = true;
                return Task.CompletedTask;
            };

            httpContext.SetEndpoint(new Endpoint(endpointFunc, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"));

            var routeOptions = Options.Create(new RouteOptions { SuppressCheckForUnhandledSecurityMetadata = true });

            RequestDelegate next = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, routeOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledEndpoint);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_ThrowsIfCorsMetadataWasFound_ButCorsMiddlewareNotInvoked()
        {
            // Arrange
            var expected = "Endpoint Test contains CORS metadata, but a middleware was not found that supports CORS." +
                Environment.NewLine +
                "Configure your application startup by adding app.UseCors() inside the call to Configure(..) in the application startup code. " +
                "The call to app.UseCors() must appear between app.UseRouting() and app.UseEndpoints(...).";
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            RequestDelegate throwIfCalled = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            httpContext.SetEndpoint(new Endpoint(throwIfCalled, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"));

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, throwIfCalled, RouteOptions);

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

            var calledEndpoint = false;
            RequestDelegate endpointFunc = (c) =>
            {
                calledEndpoint = true;
                return Task.CompletedTask;
            };

            httpContext.SetEndpoint(new Endpoint(endpointFunc, new EndpointMetadataCollection(Mock.Of<ICorsMetadata>()), "Test"));

            httpContext.Items[EndpointMiddleware.CorsMiddlewareInvokedKey] = true;

            RequestDelegate next = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, RouteOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledEndpoint);
        }

        [Fact]
        public async Task Invoke_WithEndpoint_DoesNotThrowIfUnhandledCorsAttributesWereFound_ButSuppressedViaOptions()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceProvider()
            };

            var calledEndpoint = false;
            RequestDelegate endpointFunc = (c) =>
            {
                calledEndpoint = true;
                return Task.CompletedTask;
            };

            httpContext.SetEndpoint(new Endpoint(endpointFunc, new EndpointMetadataCollection(Mock.Of<IAuthorizeData>()), "Test"));

            var routeOptions = Options.Create(new RouteOptions { SuppressCheckForUnhandledSecurityMetadata = true });

            RequestDelegate next = (c) =>
            {
                throw new InvalidTimeZoneException("Should not be called");
            };

            var middleware = new EndpointMiddleware(NullLogger<EndpointMiddleware>.Instance, next, routeOptions);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.True(calledEndpoint);
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
