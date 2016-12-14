// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class HubConnectionTests : IDisposable
    {
        private readonly TestServer _testServer;

        public HubConnectionTests()
        {
            var webHostBuilder = new WebHostBuilder().
                ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(routes =>
                    {
                        routes.MapHub<TestHub>("/hubs");
                    });
                });
            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task CheckFixedMessage()
        {
            var loggerFactory = new LoggerFactory();

            using (var httpClient = _testServer.CreateClient())
            using (var pipelineFactory = new PipelineFactory())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"), new JsonNetInvocationAdapter(), transport, httpClient, pipelineFactory, loggerFactory))
                {
                    //TODO: Get rid of this. This is to prevent "No channel" failures due to sends occuring before the first poll.
                    await Task.Delay(500);
                    var result = await connection.Invoke<string>("HelloWorld");

                    Assert.Equal("Hello World!", result);
                }
            }
        }

        [Fact]
        public async Task CanSendAndReceiveMessage()
        {
            var loggerFactory = new LoggerFactory();
            const string originalMessage = "SignalR";

            using (var httpClient = _testServer.CreateClient())
            using (var pipelineFactory = new PipelineFactory())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"), new JsonNetInvocationAdapter(), transport, httpClient, pipelineFactory, loggerFactory))
                {
                    //TODO: Get rid of this. This is to prevent "No channel" failures due to sends occuring before the first poll.
                    await Task.Delay(500);
                    var result = await connection.Invoke<string>("Echo", originalMessage);

                    Assert.Equal(originalMessage, result);
                }
            }
        }

        [Fact]
        public async Task CanInvokeClientMethodFromServer()
        {
            var loggerFactory = new LoggerFactory();
            const string originalMessage = "SignalR";

            using (var httpClient = _testServer.CreateClient())
            using (var pipelineFactory = new PipelineFactory())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"), new JsonNetInvocationAdapter(), transport, httpClient, pipelineFactory, loggerFactory))
                {
                    var tcs = new TaskCompletionSource<string>();
                    connection.On("Echo", new[] { typeof(string) }, a =>
                    {
                        tcs.TrySetResult((string)a[0]);
                    });

                    //TODO: Get rid of this. This is to prevent "No channel" failures due to sends occuring before the first poll.
                    await Task.Delay(500);
                    await connection.Invoke<Task>("CallEcho", originalMessage);
                    var completed = await Task.WhenAny(Task.Delay(2000), tcs.Task);
                    Assert.True(completed == tcs.Task, "Receive timed out!");
                    Assert.Equal(originalMessage, tcs.Task.Result);
                }
            }
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        public class TestHub : Hub
        {
            public string HelloWorld()
            {
                return "Hello World!";
            }

            public string Echo(string message)
            {
                return message;
            }

            public async Task CallEcho(string message)
            {
                await Clients.Client(Context.ConnectionId).InvokeAsync("Echo", message);
            }
        }
    }
}
