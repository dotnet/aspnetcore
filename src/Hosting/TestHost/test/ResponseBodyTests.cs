// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.TestHost.Tests
{
    public class ResponseBodyTests
    {
        [Fact]
        public async Task BodyWriter_GetMemoryAdvance_AutoCompleted()
        {
            var length = -1;
            using var host = await CreateHost(httpContext =>
            {
                var writer = httpContext.Response.BodyWriter;
                length = writer.GetMemory().Length;
                writer.Advance(length);
                return Task.CompletedTask;
            }).WithTimeout();

            using var client = host.GetTestServer().CreateClient();
            using var response = await client.GetAsync("/").WithTimeout();
            var bytes = await response.Content.ReadAsByteArrayAsync().WithTimeout();
            Assert.Equal(length, bytes.Length);
        }

        [Fact]
        public async Task BodyWriter_StartAsyncGetMemoryAdvance_AutoCompleted()
        {
            var length = -1;
            using var host = await CreateHost(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                var writer = httpContext.Response.BodyWriter;
                length = writer.GetMemory().Length;
                writer.Advance(length);
            }).WithTimeout();

            using var client = host.GetTestServer().CreateClient();
            using var response = await client.GetAsync("/").WithTimeout();
            var bytes = await response.Content.ReadAsByteArrayAsync().WithTimeout();
            Assert.Equal(length, bytes.Length);
        }

        private Task<IHost> CreateHost(RequestDelegate appDelegate)
        {
            return new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .Configure(app =>
                        {
                            app.Run(appDelegate);
                        });
                })
                .StartAsync();
        }
    }
}
