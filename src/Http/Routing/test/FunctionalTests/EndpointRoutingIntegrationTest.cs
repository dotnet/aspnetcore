// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Routing.FunctionalTests
{
    public class EndpointRoutingIntegrationTest
    {
        private static readonly RequestDelegate TestDelegate = async context => await Task.Yield();
        private static readonly string AuthErrorMessage = "Endpoint / contains authorization metadata, but a middleware was not found that supports authorization." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAuthorization() inside the call to Configure(..) in the application startup code. " +
            "The call to app.UseAuthorization() must appear between app.UseRouting() and app.UseEndpoints(...).";

        private static readonly string CORSErrorMessage = "Endpoint / contains CORS metadata, but a middleware was not found that supports CORS." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseCors() inside the call to Configure(..) in the application startup code. " +
            "The call to app.UseCors() must appear between app.UseRouting() and app.UseEndpoints(...).";

        [Fact]
        public async Task AuthorizationMiddleware_WhenNoAuthMetadataIsConfigured()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(b => b.Map("/", TestDelegate));
            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization();
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var response = await server.CreateRequest("/").SendAsync("GET");

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AuthorizationMiddleware_WhenEndpointIsNotFound()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(b => b.Map("/", TestDelegate));
            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization();
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var response = await server.CreateRequest("/not-found").SendAsync("GET");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AuthorizationMiddleware_WithAuthorizedEndpoint()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var response = await server.CreateRequest("/").SendAsync("GET");

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AuthorizationMiddleware_NotConfigured_Throws()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());

            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
            Assert.Equal(AuthErrorMessage, ex.Message);
        }

        [Fact]
        public async Task AuthorizationMiddleware_NotConfigured_WhenEndpointIsNotFound()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
            })
           .ConfigureServices(services =>
           {
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var response = await server.CreateRequest("/not-found").SendAsync("GET");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AuthorizationMiddleware_ConfiguredBeforeRouting_Throws()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseAuthorization();
                app.UseRouting();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
            Assert.Equal(AuthErrorMessage, ex.Message);
        }

        [Fact]
        public async Task AuthorizationMiddleware_ConfiguredAfterRouting_Throws()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
                app.UseAuthorization();
            })
           .ConfigureServices(services =>
           {
               services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
            Assert.Equal(AuthErrorMessage, ex.Message);
        }

        [Fact]
        public async Task CorsMiddleware_WithCorsEndpoint()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseCors();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireCors(policy => policy.AllowAnyOrigin()));
            })
           .ConfigureServices(services =>
           {
               services.AddCors();
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var response = await server.CreateRequest("/").SendAsync("PUT");

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CorsMiddleware_ConfiguredBeforeRouting_Throws()
        {
            // Arrange
            var builder = new WebHostBuilder();
            builder.Configure(app =>
            {
                app.UseCors();
                app.UseRouting();
                app.UseEndpoints(b => b.Map("/", TestDelegate).RequireCors(policy => policy.AllowAnyOrigin()));
            })
           .ConfigureServices(services =>
           {
               services.AddCors();
               services.AddRouting();
           });

            using var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
            Assert.Equal(CORSErrorMessage, ex.Message);
        }
    }
}
