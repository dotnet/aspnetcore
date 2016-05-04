// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        [Fact]
        public async Task LargeUpload()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:0/")
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

                    var response = await client.PostAsync($"http://localhost:{host.GetPort()}/", new ByteArrayContent(bytes));
                    response.EnsureSuccessStatusCode();
                    var sizeString = await response.Content.ReadAsStringAsync();
                    Assert.Equal(sizeString, bytes.Length.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Fails on Mono on Mac because it is not 64-bit.")]
        public async Task LargeMultipartUpload()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:0/")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        long total = 0;
                        var bytes = new byte[1024];
                        var count = await context.Request.Body.ReadAsync(bytes, 0, bytes.Length);
                        while (count > 0)
                        {
                            total += count;
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
                    using (var form = new MultipartFormDataContent())
                    {
                        const int oneMegabyte = 1024 * 1024;
                        const int files = 2048;
                        var bytes = new byte[oneMegabyte];

                        for (int i = 0; i < files; i++)
                        {
                            var fileName = Guid.NewGuid().ToString();
                            form.Add(new ByteArrayContent(bytes), "file", fileName);
                        }

                        var length = form.Headers.ContentLength.Value;
                        var response = await client.PostAsync($"http://localhost:{host.GetPort()}/", form);
                        response.EnsureSuccessStatusCode();
                        Assert.Equal(length.ToString(CultureInfo.InvariantCulture), await response.Content.ReadAsStringAsync());
                    }
                }
            }
        }

        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("localhost", "127.0.0.1")]
        public Task RemoteIPv4Address(string requestAddress, string expectAddress)
        {
            return TestRemoteIPAddress("localhost", requestAddress, expectAddress);
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public Task RemoteIPv6Address()
        {
            return TestRemoteIPAddress("[::1]", "[::1]", "::1");
        }

        [Fact]
        public async Task DoesNotHangOnConnectionCloseRequest()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:0")
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

                var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public void RequestPathIsNormalized()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:0/\u0041\u030A")
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

                using (var socket = TestConnection.CreateConnectedLoopbackSocket(host.GetPort()))
                {
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
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://{registerAddress}:0")
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

                var response = await client.GetAsync($"http://{requestAddress}:{host.GetPort()}/");
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
