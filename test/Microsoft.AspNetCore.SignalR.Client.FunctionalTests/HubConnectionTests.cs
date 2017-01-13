// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private static readonly bool _verbose = string.Equals(Environment.GetEnvironmentVariable("SIGNALR_TEST_VERBOSE"), "1");

        public HubConnectionTests()
        {
            var webHostBuilder = new WebHostBuilder().
                ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .ConfigureLogging(loggerFactory =>
                {
                    if (_verbose)
                    {
                        loggerFactory.AddConsole();
                    }
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
            var loggerFactory = CreateLogger();

            using (var httpClient = _testServer.CreateClient())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"),
                    new JsonNetInvocationAdapter(), transport, httpClient, loggerFactory))
                {
                    EnsureConnectionEstablished(connection);

                    var result = await connection.Invoke<string>("HelloWorld");

                    Assert.Equal("Hello World!", result);
                }
            }
        }

        [Fact]
        public async Task CanSendAndReceiveMessage()
        {
            var loggerFactory = CreateLogger();
            const string originalMessage = "SignalR";

            using (var httpClient = _testServer.CreateClient())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"),
                    new JsonNetInvocationAdapter(), transport, httpClient, loggerFactory))
                {
                    EnsureConnectionEstablished(connection);

                    var result = await connection.Invoke<string>("Echo", originalMessage);

                    Assert.Equal(originalMessage, result);
                }
            }
        }

        [Fact]
        public async Task CanInvokeClientMethodFromServer()
        {
            var loggerFactory = CreateLogger();
            const string originalMessage = "SignalR";

            using (var httpClient = _testServer.CreateClient())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"),
                    new JsonNetInvocationAdapter(), transport, httpClient, loggerFactory))
                {
                    var tcs = new TaskCompletionSource<string>();
                    connection.On("Echo", new[] { typeof(string) }, a =>
                    {
                        tcs.TrySetResult((string)a[0]);
                    });

                    EnsureConnectionEstablished(connection);

                    await connection.Invoke<Task>("CallEcho", originalMessage);
                    var completed = await Task.WhenAny(Task.Delay(2000), tcs.Task);
                    Assert.True(completed == tcs.Task, "Receive timed out!");
                    Assert.Equal(originalMessage, tcs.Task.Result);
                }
            }
        }

        [Fact]
        public async Task ServerClosesConnectionIfHubMethodCannotBeResolved()
        {
            var loggerFactory = CreateLogger();

            using (var httpClient = _testServer.CreateClient())
            {
                var transport = new LongPollingTransport(httpClient, loggerFactory);
                using (var connection = await HubConnection.ConnectAsync(new Uri("http://test/hubs"),
                    new JsonNetInvocationAdapter(), transport, httpClient, loggerFactory))
                {
                    EnsureConnectionEstablished(connection);

                    var ex = await Assert.ThrowsAnyAsync<Exception>(
                        async () => await connection.Invoke<object>("!@#$%"));

                    Assert.Equal(ex.Message, "Unknown hub method '!@#$%'");
                }
            }
        }

        private static void EnsureConnectionEstablished(HubConnection connection)
        {
            if (connection.Completion.IsCompleted)
            {
                connection.Completion.GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        private static LoggerFactory CreateLogger()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(_verbose ? LogLevel.Trace : LogLevel.Error);

            return loggerFactory;
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
