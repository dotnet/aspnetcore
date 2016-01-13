// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task LargeUpload()
        {
            var port = PortManager.GetPort();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", $"http://localhost:{port}/" }
                })
                .Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        // Read the full request body
                        var total = 0;
                        var bytes = new byte[1024];
                        var count = await context.Request.Body.ReadAsync(bytes, 0, bytes.Length);
                        while (count > 0)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                Assert.Equal(total % 256, bytes[i]);
                                total++;
                            }
                            count = await context.Request.Body.ReadAsync(bytes, 0, bytes.Length);
                        }

                        await context.Response.WriteAsync(total.ToString(CultureInfo.InvariantCulture));
                    });
                });

            using (var host = builder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var bytes = new byte[1024 * 1024];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = (byte)i;
                    }

                    var response = await client.PostAsync($"http://localhost:{port}/", new ByteArrayContent(bytes));
                    response.EnsureSuccessStatusCode();
                    var sizeString = await response.Content.ReadAsStringAsync();
                    Assert.Equal(sizeString, bytes.Length.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        [ConditionalTheory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("localhost", "127.0.0.1")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public Task RemoteIPv4Address(string requestAddress, string expectAddress)
        {
            return TestRemoteIPAddress("localhost", requestAddress, expectAddress);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        [IPv6SupportedCondition]
        public Task RemoteIPv6Address()
        {
            return TestRemoteIPAddress("[::1]", "[::1]", "::1");
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task DoesNotHangOnConnectionCloseRequest()
        {
            var port = PortManager.GetPort();
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", $"http://localhost:{port}" }
                }).Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var connection = context.Connection;
                        await context.Response.WriteAsync("hello, world");
                    });
                });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                host.Start();

                client.DefaultRequestHeaders.Connection.Clear();
                client.DefaultRequestHeaders.Connection.Add("close");

                var response = await client.GetAsync($"http://localhost:{port}/");
                response.EnsureSuccessStatusCode();
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task RequestPathIsNormalized()
        {
            var port = PortManager.GetPort();
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", $"http://localhost:{port}/\u0041\u030A" }
                }).Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var connection = context.Connection;
                        Assert.Equal("/\u00C5", context.Request.PathBase.Value);
                        Assert.Equal("/B/\u00C5", context.Request.Path.Value);
                        await context.Response.WriteAsync("hello, world");
                    });
                });

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
                    socket.Send(Encoding.ASCII.GetBytes("GET /%41%CC%8A/A/../B/%41%CC%8A HTTP/1.1\r\n\r\n"));
                    socket.Shutdown(SocketShutdown.Send);

                    var response = new StringBuilder();
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var length = socket.Receive(buffer);
                        if (length == 0)
                        {
                            break;
                        }
                        
                        response.Append(Encoding.ASCII.GetString(buffer, 0, length));
                    }
                    
                    Assert.StartsWith("HTTP/1.1 200 OK", response.ToString());
                }
            }
        }

        private async Task TestRemoteIPAddress(string registerAddress, string requestAddress, string expectAddress)
        {
            var port = PortManager.GetPort();
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", $"http://{registerAddress}:{port}" }
                }).Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var connection = context.Connection;
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            RemoteIPAddress = connection.RemoteIpAddress?.ToString(),
                            RemotePort = connection.RemotePort,
                            LocalIPAddress = connection.LocalIpAddress?.ToString(),
                            LocalPort = connection.LocalPort
                        }));
                    });
                });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                host.Start();

                var response = await client.GetAsync($"http://{requestAddress}:{port}/");
                response.EnsureSuccessStatusCode();

                var connectionFacts = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(connectionFacts);

                var facts = JsonConvert.DeserializeObject<JObject>(connectionFacts);
                Assert.Equal(expectAddress, facts["RemoteIPAddress"].Value<string>());
                Assert.NotEmpty(facts["RemotePort"].Value<string>());
            }
        }
    }
}
