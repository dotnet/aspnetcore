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
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sockets.BindTests
{
    public class SocketTransportOptionsTests : LoggedTestBase
    {
        [Theory]
        [MemberData(nameof(GetEndpoints))]
        public async Task SocketTransportCallsCreateBoundListenSocket(EndPoint endpointToTest)
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
        public void CreateDefaultBoundListenSocket_BindsForAllEndPoints(EndPoint endpoint)
        {
            using var listenSocket = SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
            Assert.NotNull(listenSocket.LocalEndPoint);
        }

        // static to ensure that the underlying handle doesn't get disposed
        // when a local reference is GCed by the iterator in GetEndPoints
        private static Socket _fileHandleSocket;

        public static IEnumerable<object[]> GetEndpoints()
        {
            // IPv4
            yield return new object[] {new IPEndPoint(IPAddress.Loopback, 0)};
            // IPv6
            yield return new object[] {new IPEndPoint(IPAddress.IPv6Loopback, 0)};
            // Unix sockets
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[]
                {
                    new UnixDomainSocketEndPoint($"/tmp/{DateTime.UtcNow:yyyyMMddTHHmmss.fff}.sock")
                };
            }

            // file handle
            // slightly messy but allows us to create a FileHandleEndPoint
            // from the underlying OS handle used by the socket
            _fileHandleSocket = new(
                AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp
            );
            _fileHandleSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            yield return new object[]
            {
                new FileHandleEndPoint((ulong) _fileHandleSocket.Handle, FileHandleType.Auto)
            };

            // TODO: other endpoint types?
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
}
