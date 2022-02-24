// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class TestServerTests : VerifiableLoggedTest
{
    [Fact]
    public async Task WebSocketsWorks()
    {
        using (StartVerifiableLog())
        {
            var builder = new WebHostBuilder().ConfigureServices(s =>
            {
                s.AddLogging();
                s.AddSingleton(LoggerFactory);
                s.AddSignalR();
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<EchoHub>("/echo");
                });
            });
            var server = new TestServer(builder);

            var webSocketFactoryCalled = false;
            var connectionBuilder = new HubConnectionBuilder()
                .WithUrl(server.BaseAddress + "echo", options =>
                {
                    options.Transports = Http.Connections.HttpTransportType.WebSockets;
                    options.HttpMessageHandlerFactory = _ =>
                    {
                        return server.CreateHandler();
                    };
                    options.WebSocketFactory = async (context, token) =>
                    {
                        webSocketFactoryCalled = true;
                        var wsClient = server.CreateWebSocketClient();
                        return await wsClient.ConnectAsync(context.Uri, default);
                    };
                });
            connectionBuilder.Services.AddLogging();
            connectionBuilder.Services.AddSingleton(LoggerFactory);
            var connection = connectionBuilder.Build();

            var originalMessage = "message";
            connection.On<string>("Echo", (receivedMessage) =>
            {
                Assert.Equal(originalMessage, receivedMessage);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("Echo", originalMessage);
            Assert.True(webSocketFactoryCalled);
        }
    }

    [Fact]
    public async Task LongPollingWorks()
    {
        using (StartVerifiableLog())
        {
            var builder = new WebHostBuilder().ConfigureServices(s =>
            {
                s.AddLogging();
                s.AddSingleton(LoggerFactory);
                s.AddSignalR();
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<EchoHub>("/echo");
                });
            });
            var server = new TestServer(builder);

            var connectionBuilder = new HubConnectionBuilder()
                .WithUrl(server.BaseAddress + "echo", options =>
                {
                    options.Transports = Http.Connections.HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ =>
                    {
                        return server.CreateHandler();
                    };
                });
            connectionBuilder.Services.AddLogging();
            connectionBuilder.Services.AddSingleton(LoggerFactory);
            var connection = connectionBuilder.Build();

            var originalMessage = "message";
            connection.On<string>("Echo", (receivedMessage) =>
            {
                Assert.Equal(originalMessage, receivedMessage);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("Echo", originalMessage);
        }
    }
}

class EchoHub : Hub
{
    public Task Echo(string message)
    {
        return Clients.All.SendAsync("Echo", message);
    }
}
