// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
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
            using var host = await CreateHost(httpContext =>
            {
                var writer = httpContext.Response.BodyWriter;
                writer.GetMemory(100);
                writer.Advance(100);
                return Task.CompletedTask;
            });

            var response = await host.GetTestServer().CreateClient().GetAsync("/");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(100, bytes.Length);
        }

        [Fact]
        public async Task BodyWriter_StartAsyncGetMemoryAdvance_AutoCompleted()
        {
            using var host = await CreateHost(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                var writer = httpContext.Response.BodyWriter;
                writer.GetMemory(100);
                writer.Advance(100);
            });

            var response = await host.GetTestServer().CreateClient().GetAsync("/");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(100, bytes.Length);
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
