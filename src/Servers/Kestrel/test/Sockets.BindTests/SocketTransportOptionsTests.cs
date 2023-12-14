// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sockets.BindTests;

public class SocketTransportOptionsTests : LoggedTestBase
{
    private async Task VerifySocketTransportCallsCreateBoundListenSocketAsync(EndPoint endpointToTest)
    {
        var wasCalled = false;

        Socket CreateListenSocket(EndPoint endpoint)
        {
            wasCalled = true;
            return SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
        }

        using var host = CreateWebHost(
            endpointToTest,
            options =>
            {
                options.CreateBoundListenSocket = CreateListenSocket;
            }
        );

        await host.StartAsync();
        Assert.True(wasCalled, $"Expected {nameof(SocketTransportOptions.CreateBoundListenSocket)} to be called.");
        await host.StopAsync();
    }

    [Theory]
    [MemberData(nameof(GetEndpoints))]
    public Task SocketTransportCallsCreateBoundListenSocketForNewEndpoints(EndPoint endpointToTest)
    {
        return VerifySocketTransportCallsCreateBoundListenSocketAsync(endpointToTest);
    }

    [Fact]
    public async Task SocketTransportCallsCreateBoundListenSocketForFileHandleEndpoint()
    {
        using var fileHandleSocket = CreateBoundSocket();
        var endpoint = new FileHandleEndPoint((ulong)fileHandleSocket.Handle, FileHandleType.Auto);

        await VerifySocketTransportCallsCreateBoundListenSocketAsync(endpoint);
    }

    [Theory]
    [MemberData(nameof(GetEndpoints))]
    public void CreateDefaultBoundListenSocket_BindsForNewEndPoints(EndPoint endpoint)
    {
        using var listenSocket = SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
        Assert.NotNull(listenSocket.LocalEndPoint);
    }

    [Fact]
    public void CreateDefaultBoundListenSocket_PreservesLocalEndpointFromFileHandleEndpoint()
    {
        using var fileHandleSocket = CreateBoundSocket();
        var endpoint = new FileHandleEndPoint((ulong)fileHandleSocket.Handle, FileHandleType.Auto);

        using var listenSocket = SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);

        Assert.NotNull(fileHandleSocket.LocalEndPoint);
        Assert.Equal(fileHandleSocket.LocalEndPoint, listenSocket.LocalEndPoint);
    }

    public static IEnumerable<object[]> GetEndpoints()
    {
        // IPv4
        yield return new object[] { new IPEndPoint(IPAddress.Loopback, 0) };
        // IPv6
        yield return new object[] { new IPEndPoint(IPAddress.IPv6Loopback, 0) };
        // Unix sockets
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new object[]
            {
                    new UnixDomainSocketEndPoint($"/tmp/{DateTime.UtcNow:yyyyMMddTHHmmss.fff}.sock")
            };
        }

        // TODO: other endpoint types?
    }

    private static Socket CreateBoundSocket()
    {
        // file handle
        // slightly messy but allows us to create a FileHandleEndPoint
        // from the underlying OS handle used by the socket
        var fileHandleSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        fileHandleSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return fileHandleSocket;
    }

    private IHost CreateWebHost(EndPoint endpoint, Action<SocketTransportOptions> configureSocketOptions) =>
        TransportSelector.GetHostBuilder()
            .ConfigureWebHost(
                webHostBuilder =>
                {
                    webHostBuilder
                        .UseSockets(configureSocketOptions)
                        .UseKestrel(options => options.Listen(endpoint))
                        .Configure(
                            app => app.Run(ctx => ctx.Response.WriteAsync("Hello World"))
                        );
                }
            )
            .ConfigureServices(AddTestLogging)
            .Build();
}
