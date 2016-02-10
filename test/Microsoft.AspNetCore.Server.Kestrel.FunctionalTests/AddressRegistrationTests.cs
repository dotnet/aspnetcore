// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class AddressRegistrationTests
    {
        [Theory, MemberData(nameof(AddressRegistrationDataIPv4))]
        public async Task RegisterAddresses_IPv4_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [Theory, MemberData(nameof(AddressRegistrationDataIPv6))]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        public async Task RegisterAddresses_Success(string addressInput, string[] testUrls)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", addressInput }
                })
                .Build();

            var hostBuilder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    foreach (var testUrl in testUrls)
                    {
                        var responseText = await client.GetStringAsync(testUrl);
                        Assert.Equal(testUrl, responseText);
                    }
                }
            }
        }

        public static TheoryData<string, string[]> AddressRegistrationDataIPv4
        {
            get
            {
                var port1 = PortManager.GetPort();
                var port2 = PortManager.GetPort();
                var dataset = new TheoryData<string, string[]>();
                dataset.Add($"{port1}", new[] { $"http://localhost:{port1}/" });
                dataset.Add($"{port1};{port2}", new[] { $"http://localhost:{port1}/", $"http://localhost:{port2}/" });
                dataset.Add($"http://127.0.0.1:{port1}/", new[] { $"http://127.0.0.1:{port1}/", });
                dataset.Add($"http://localhost:{port1}/base/path", new[] { $"http://localhost:{port1}/base/path" });

                return dataset;
            }
        }

        public static TheoryData<string, string[]> AddressRegistrationDataIPv6
        {
            get
            {
                var port = PortManager.GetPort();
                var dataset = new TheoryData<string, string[]>();
                dataset.Add($"http://*:{port}/", new[] { $"http://localhost:{port}/", $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });
                dataset.Add($"http://localhost:{port}/", new[] { $"http://localhost:{port}/", $"http://127.0.0.1:{port}/",
                    /* // https://github.com/aspnet/KestrelHttpServer/issues/231
                    $"http://[::1]:{port}/"
                    */ });
                dataset.Add($"http://[::1]:{port}/", new[] { $"http://[::1]:{port}/", });
                dataset.Add($"http://127.0.0.1:{port}/;http://[::1]:{port}/", new[] { $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });

                return dataset;
            }
        }

        private void ConfigureEchoAddress(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync(context.Request.GetDisplayUrl());
            });
        }
    }
}
