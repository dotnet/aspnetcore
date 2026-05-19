// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Xunit;
using KestrelHttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using KestrelHttpVersion = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion;

namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;

public class SocketTransportTests : LoggedTestBase
{
    [Fact]
    public async Task SocketTransportExposesSocketsFeature()
    {
        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseUrls("http://127.0.0.1:0")
                    .Configure(app =>
                    {
                        app.Run(context =>
                        {
                            var socket = context.Features.Get<IConnectionSocketFeature>().Socket;
                            Assert.NotNull(socket);
                            Assert.Equal(ProtocolType.Tcp, socket.ProtocolType);
                            var ip = (IPEndPoint)socket.RemoteEndPoint;
                            Assert.Equal(ip.Address, context.Connection.RemoteIpAddress);
                            Assert.Equal(ip.Port, context.Connection.RemotePort);

                            return Task.CompletedTask;
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using var host = builder.Build();
        using var client = new HttpClient();

        await host.StartAsync();

        var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
        response.EnsureSuccessStatusCode();

        await host.StopAsync();
    }
}
