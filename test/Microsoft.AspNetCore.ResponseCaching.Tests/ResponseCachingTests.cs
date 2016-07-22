// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingTests
    {
        [Fact]
        public async void ServesCachedContentIfAvailable()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDistributedResponseCache();
                })
                .Configure(app =>
                {
                    app.UseResponseCaching();
                    app.Run(async (context) =>
                    {
                        context.Response.Headers["Cache-Control"] = "public";
                        await context.Response.WriteAsync(DateTime.UtcNow.ToString());
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var initialResponse = await client.GetAsync("");
                var subsequentResponse = await client.GetAsync("");

                initialResponse.EnsureSuccessStatusCode();
                subsequentResponse.EnsureSuccessStatusCode();

                // TODO: Check for the appropriate headers once we actually set them
                Assert.False(initialResponse.Headers.Contains("Served_From_Cache"));
                Assert.True(subsequentResponse.Headers.Contains("Served_From_Cache"));
                Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }
    }
}
