using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class SocketTransportOptionsTests : LoggedTestBase
    {
        [Theory]
        [MemberData(nameof(GetEndpoints))]
        public async Task SocketTransportCallsConfigureListenSocket(EndPoint endpointToTest)
        {
            var wasCalled = false;
            void ConfigureListenSocket(EndPoint endpoint, Socket socket) => wasCalled = true;

            using var host = CreateWebHost(
                endpointToTest, options => options.ConfigureListenSocket = ConfigureListenSocket
            );
            await host.StartAsync();
            Assert.True(wasCalled, $"Expected {nameof(SocketTransportOptions.ConfigureListenSocket)} to be called.");
            await host.StopAsync();
        }

        [Theory]
        [MemberData(nameof(GetEndpoints))]
        public async Task SocketTransportCallsConfigureAcceptSocket(EndPoint endpointToTest)
        {
            var wasCalled = false;
            void ConfigureAcceptSocket(EndPoint endpoint, Socket socket) => wasCalled = true;

            using var host = CreateWebHost(
                endpointToTest, options => options.ConfigureAcceptSocket = ConfigureAcceptSocket
            );
            using var client = CreateHttpClient(endpointToTest);
            await host.StartAsync();
            var uri = host.GetUris().First();
            var response = await client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            Assert.True(wasCalled, $"Expected {nameof(SocketTransportOptions.ConfigureAcceptSocket)} to be called.");
            await host.StopAsync();
        }

        private static int _counter = 1;
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
                    // NOTE
                    // to avoid "address in use" errors need to ensure
                    // the socket path is unique
                    new UnixDomainSocketEndPoint(
                        $"/tmp/test-{Interlocked.Increment(ref _counter)}.sock"
                    )
                };
            }

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
        private static HttpClient CreateHttpClient(EndPoint endpoint)
        {
            if (endpoint is UnixDomainSocketEndPoint)
            {
                // https://stackoverflow.com/a/67203488/871146
                return new HttpClient(new SocketsHttpHandler
                {
                    ConnectCallback = async (_, _) =>
                    {
                        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                        await socket.ConnectAsync(endpoint);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                });
            }

            return new HttpClient();
        }
    }
}
