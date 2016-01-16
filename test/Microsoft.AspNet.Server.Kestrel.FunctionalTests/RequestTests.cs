// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task LargeUpload()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", "http://localhost:8791/" }
                })
                .Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNet.Server.Kestrel")
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

                    var response = await client.PostAsync("http://localhost:8791/", new ByteArrayContent(bytes));
                    response.EnsureSuccessStatusCode();
                    var sizeString = await response.Content.ReadAsStringAsync();
                    Assert.Equal(sizeString, bytes.Length.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        [ConditionalTheory]
        [InlineData("127.0.0.1", "127.0.0.1", "8792")]
        [InlineData("localhost", "127.0.0.1", "8792")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public Task RemoteIPv4Address(string requestAddress, string expectAddress, string port)
        {
            return TestRemoteIPAddress("localhost", requestAddress, expectAddress, port);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        [IPv6SupportedCondition]
        public Task RemoteIPv6Address()
        {
            return TestRemoteIPAddress("[::1]", "[::1]", "::1", "8792");
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task DoesNotHangOnConnectionCloseRequest()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", "http://localhost:8791" }
                }).Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNet.Server.Kestrel")
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

                var response = await client.GetAsync("http://localhost:8791/");
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task TestRemoteIPAddress(string registerAddress, string requestAddress, string expectAddress, string port)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", $"http://{registerAddress}:{port}" }
                }).Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNet.Server.Kestrel")
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
