// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class AddressRegistrationTests
    {
        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv4))]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public async Task RegisterAddresses_IPv4_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6))]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
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

            var applicationBuilder = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseServerFactory("Microsoft.AspNet.Server.Kestrel")
                .Configure(ConfigureEchoAddress);

            using (var app = applicationBuilder.Build().Start())
            {
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
                var dataset = new TheoryData<string, string[]>();
                dataset.Add("8787", new[] { "http://localhost:8787/" });
                dataset.Add("8787;8788", new[] { "http://localhost:8787/", "http://localhost:8788/" });
                dataset.Add("http://127.0.0.1:8787/", new[] { "http://127.0.0.1:8787/", });
                dataset.Add("http://localhost:8787/base/path", new[] { "http://localhost:8787/base/path" });

                return dataset;
            }
        }

        public static TheoryData<string, string[]> AddressRegistrationDataIPv6
        {
            get
            {
                var dataset = new TheoryData<string, string[]>();
                dataset.Add("http://*:8787/", new[] { "http://localhost:8787/", "http://127.0.0.1:8787/", "http://[::1]:8787/" });
                dataset.Add("http://localhost:8787/", new[] { "http://localhost:8787/", "http://127.0.0.1:8787/",
                    /* // https://github.com/aspnet/KestrelHttpServer/issues/231
                    "http://[::1]:8787/"
                    */ });
                dataset.Add("http://[::1]:8787/", new[] { "http://[::1]:8787/", });
                dataset.Add("http://127.0.0.1:8787/;http://[::1]:8787/", new[] { "http://127.0.0.1:8787/", "http://[::1]:8787/" });

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
