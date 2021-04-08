using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sockets.FunctionalTests
{
    public class SocketTranspotTests : LoggedTestBase
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
}
