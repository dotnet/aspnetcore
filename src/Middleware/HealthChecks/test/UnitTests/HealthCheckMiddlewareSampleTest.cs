// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public class HealthCheckMiddlewareSampleTest
    {
        [Fact]
        public async Task BasicStartup()
        {
            var builder = new WebHostBuilder()
                .UseStartup<HealthChecksSample.BasicStartup>();

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CustomWriterStartup()
        {
            var builder = new WebHostBuilder()
                .UseStartup<HealthChecksSample.CustomWriterStartup>();

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.ToString());

            // Ignoring the body since it contains a bunch of statistics
        }

        [Fact]
        public async Task LivenessProbeStartup_Liveness()
        {
            var builder = new WebHostBuilder()
                .UseStartup<HealthChecksSample.LivenessProbeStartup>();

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LivenessProbeStartup_Readiness()
        {
            var builder = new WebHostBuilder()
                .UseStartup<HealthChecksSample.LivenessProbeStartup>();

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        }
    }
}
