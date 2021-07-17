using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

using KestrelHttpVersion = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion;
using KestrelHttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

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
            using var client = new System.Net.Http.HttpClient();

            await host.StartAsync();

            var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
            response.EnsureSuccessStatusCode();

            await host.StopAsync();
        }

        [Fact]
        public async Task CanReadFromSocketSocketsFeatureInConnectionMiddleware()
        {
            var builder = TransportSelector.GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(options =>
                        {
                            options.ListenAnyIP(0, lo =>
                            {
                                lo.Use(next =>
                                {
                                    return async connection =>
                                    {
                                        var socket = connection.Features.Get<IConnectionSocketFeature>().Socket;
                                        Assert.NotNull(socket);

                                        var buffer = new byte[4096];

                                        var read = await socket.ReceiveAsync(buffer, SocketFlags.None);

                                        static void ParseHttp(ReadOnlySequence<byte> data)
                                        {
                                            var parser = new HttpParser<ParserHandler>();
                                            var handler = new ParserHandler();

                                            var reader = new SequenceReader<byte>(data);

                                            // Assume we can parse the HTTP request in a single buffer
                                            Assert.True(parser.ParseRequestLine(handler, ref reader));
                                            Assert.True(parser.ParseHeaders(handler, ref reader));

                                            Assert.Equal(KestrelHttpMethod.Get, handler.HttpMethod);
                                            Assert.Equal(KestrelHttpVersion.Http11, handler.HttpVersion);
                                        }

                                        ParseHttp(new ReadOnlySequence<byte>(buffer[0..read]));

                                        await socket.SendAsync(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nConnection: close\r\n\r\n"), SocketFlags.None);
                                    };
                                });
                            });
                        })
                        .Configure(app => { });
                })
                .ConfigureServices(AddTestLogging);

            using var host = builder.Build();
            using var client = new HttpClient();

            await host.StartAsync();

            var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
            response.EnsureSuccessStatusCode();

            await host.StopAsync();
        }

        private class ParserHandler : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public KestrelHttpVersion HttpVersion { get; set; }
            public KestrelHttpMethod HttpMethod { get; set; }
            public Dictionary<string, string> Headers = new();

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                Headers[Encoding.ASCII.GetString(name)] = Encoding.ASCII.GetString(value);
            }

            public void OnHeadersComplete(bool endStream)
            {
            }

            public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
            {
                HttpMethod = versionAndMethod.Method;
                HttpVersion = versionAndMethod.Version;
            }

            public void OnStaticIndexedHeader(int index)
            {
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
            }
        }
    }
}
