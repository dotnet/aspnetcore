// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class RequestLifetimeTests
    {
        [Fact]
        public async Task LifetimeFeature_Abort_TriggersRequestAbortedToken()
        {
            var requestAborted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = await CreateHost(async httpContext =>
            {
                httpContext.RequestAborted.Register(() => requestAborted.SetResult(0));
                httpContext.Abort();

                await requestAborted.Task.WithTimeout();
            });

            var client = host.GetTestServer().CreateClient();
            var ex = await Assert.ThrowsAsync<Exception>(() => client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead));
            Assert.Equal("The application aborted the request.", ex.Message);
            await requestAborted.Task.WithTimeout();
        }

        [Fact]
        public async Task LifetimeFeature_AbortBeforeHeadersSent_ClientThrows()
        {
            var abortReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = await CreateHost(async httpContext =>
            {
                httpContext.Abort();
                await abortReceived.Task.WithTimeout();
            });

            var client = host.GetTestServer().CreateClient();
            var ex = await Assert.ThrowsAsync<Exception>(() => client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead));
            Assert.Equal("The application aborted the request.", ex.Message);
            abortReceived.SetResult(0);
        }

        [Fact]
        public async Task LifetimeFeature_AbortAfterHeadersSent_ClientBodyThrows()
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var abortReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = await CreateHost(async httpContext =>
            {
                await httpContext.Response.Body.FlushAsync();
                await responseReceived.Task.WithTimeout();
                httpContext.Abort();
                await abortReceived.Task.WithTimeout();
            });

            var client = host.GetTestServer().CreateClient();
            var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
            responseReceived.SetResult(0);
            response.EnsureSuccessStatusCode();
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
            var rex = ex.GetBaseException();
            Assert.Equal("The application aborted the request.", rex.Message);
            abortReceived.SetResult(0);
        }

        [Fact]
        public async Task LifetimeFeature_AbortAfterSomeDataSent_ClientBodyThrows()
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var abortReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = await CreateHost(async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello World");
                await responseReceived.Task.WithTimeout();
                httpContext.Abort();
                await abortReceived.Task.WithTimeout();
            });

            var client = host.GetTestServer().CreateClient();
            using var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
            responseReceived.SetResult(0);
            response.EnsureSuccessStatusCode();
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
            var rex = ex.GetBaseException();
            Assert.Equal("The application aborted the request.", rex.Message);
            abortReceived.SetResult(0);
        }

        // TODO: Abort after CompleteAsync - No-op, the request is already complete.

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
