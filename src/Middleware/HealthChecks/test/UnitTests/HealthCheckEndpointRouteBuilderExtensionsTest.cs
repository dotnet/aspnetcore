// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public class HealthCheckEndpointRouteBuilderExtensionsTest
    {
        [Fact]
        public async Task MapHealthChecks_ReturnsOk()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRouting(routes =>
                    {
                        routes.MapHealthChecks("/healthz");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/healthz");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task MapHealthChecks_WithOptions_ReturnsOk()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRouting(routes =>
                    {
                        routes.MapHealthChecks("/healthz", new HealthCheckOptions
                        {
                            ResponseWriter = async (context, report) =>
                            {
                                context.Response.ContentType = "text/plain";
                                await context.Response.WriteAsync("Custom!");
                            }
                        });
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/healthz");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Custom!", await response.Content.ReadAsStringAsync());
        }
    }
}
