// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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
                        routes.MapHub<TestHub>("hubs");
                    });
                });
            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task CheckFixedMessage()
        {
            var loggerFactory = CreateLogger();

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var result = await connection.Invoke<string>(nameof(TestHub.HelloWorld));

                Assert.Equal("Hello World!", result);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task CanSendAndReceiveMessage()
        {
            var loggerFactory = CreateLogger();
            const string originalMessage = "SignalR";

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var result = await connection.Invoke<string>(nameof(TestHub.Echo), originalMessage);

                Assert.Equal(originalMessage, result);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task MethodsAreCaseInsensitive()
        {
            var loggerFactory = CreateLogger();
            const string originalMessage = "SignalR";

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var result = await connection.Invoke<string>(nameof(TestHub.Echo).ToLowerInvariant(), originalMessage);

                Assert.Equal(originalMessage, result);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task CanInvokeClientMethodFromServer()
        {
            var loggerFactory = CreateLogger();
            const string originalMessage = "SignalR";

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var tcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", tcs.SetResult);

                await connection.Invoke(nameof(TestHub.CallEcho), originalMessage).OrTimeout();

                Assert.Equal(originalMessage, await tcs.Task.OrTimeout());
            }
            finally
            {
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task CanStreamClientMethodFromServer()
        {
            var loggerFactory = CreateLogger();

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var tcs = new TaskCompletionSource<string>();

                var results = await connection.Stream<string>(nameof(TestHub.Stream)).ReadAllAsync().OrTimeout();

                Assert.Equal(new[] { "a", "b", "c" }, results.ToArray());
            }
            finally
            {
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task ServerClosesConnectionIfHubMethodCannotBeResolved()
        {
            var loggerFactory = CreateLogger();

            var httpConnection = new HttpConnection(new Uri("http://test/hubs"), TransportType.LongPolling, loggerFactory, _testServer.CreateHandler());
            var connection = new HubConnection(httpConnection, loggerFactory);
            try
            {
                await connection.StartAsync();

                var ex = await Assert.ThrowsAnyAsync<Exception>(
                    async () => await connection.Invoke("!@#$%"));

                Assert.Equal("Unknown hub method '!@#$%'", ex.Message);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        private static ILoggerFactory CreateLogger()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddFilter<ConsoleLoggerProvider>(level => level >= (_verbose ? LogLevel.Trace : LogLevel.Error));
                builder.AddDebug();
            });

            return serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
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

            public IObservable<string> Stream()
            {
                return new[] { "a", "b", "c" }.ToObservable();
            }
        }
    }
}
