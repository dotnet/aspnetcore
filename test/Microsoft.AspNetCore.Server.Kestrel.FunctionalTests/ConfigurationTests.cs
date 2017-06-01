// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ConfigurationTests
    {
        private void ConfigureEchoAddress(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync(context.Request.GetDisplayUrl());
            });
        }

        [Fact]
        public void BindsKestrelToInvalidIp_FailsToStart()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Address", "ABCDEFGH" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Port", "0" }
                }).Build())
                .ConfigureServices(services =>
                {
                    // Microsoft.AspNetCore.dll does this
                    services.AddTransient(typeof(IConfigureOptions<>), typeof(ConfigureDefaults<>));
                })
                .Configure(ConfigureEchoAddress);

            Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
        }

        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("::1", "[::1]")]
        public async Task BindsKestrelHttpEndPointFromConfiguration(string endPointAddress, string requestAddress)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Address", $"{endPointAddress}" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Port", "0" }
                }).Build())
                .ConfigureServices(services =>
                {
                    // Microsoft.AspNetCore.dll does this
                    services.AddTransient(typeof(IConfigureOptions<>), typeof(ConfigureDefaults<>));
                })
                .Configure(ConfigureEchoAddress);
            
            using (var webHost = hostBuilder.Start())
            {
                var port = GetWebHostPort(webHost);

                Assert.NotEqual(5000, port); // Default port

                Assert.NotEqual(0, port);

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync($"http://{requestAddress}:{port}");
                    Assert.Equal($"http://{requestAddress}:{port}/", response);
                }
            }
        }

        [Fact]
        public async Task BindsKestrelHttpsEndPointFromConfiguration_ReferencedCertificateFile()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Address", "127.0.0.1" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Port", "0" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Certificate", "TestCert" },
                    { "Certificates:TestCert:Source", "File" },
                    { "Certificates:TestCert:Path", "testCert.pfx" },
                    { "Certificates:TestCert:Password", "testPassword" }
                }).Build())
                .ConfigureServices(services =>
                {
                    // Microsoft.AspNetCore.dll does this
                    services.AddTransient(typeof(IConfigureOptions<>), typeof(ConfigureDefaults<>));
                })
                .Configure(ConfigureEchoAddress);
            
            using (var webHost = hostBuilder.Start())
            {
                var port = GetWebHostPort(webHost);

                var response = await HttpClientSlim.GetStringAsync($"https://127.0.0.1:{port}", validateCertificate: false);
                Assert.Equal($"https://127.0.0.1:{port}/", response);
            }
        }

        [Fact]
        public async Task BindsKestrelHttpsEndPointFromConfiguration_InlineCertificateFile()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Address", "127.0.0.1" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Port", "0" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Certificate:Source", "File" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Certificate:Path", "testCert.pfx" },
                    { "Microsoft:AspNetCore:Server:Kestrel:Endpoints:0:Certificate:Password", "testPassword" }
                }).Build())
                .ConfigureServices(services =>
                {
                    // Microsoft.AspNetCore.dll does this
                    services.AddTransient(typeof(IConfigureOptions<>), typeof(ConfigureDefaults<>));
                })
                .Configure(ConfigureEchoAddress);

            using (var webHost = hostBuilder.Start())
            {
                var port = GetWebHostPort(webHost);

                var response = await HttpClientSlim.GetStringAsync($"https://127.0.0.1:{port}", validateCertificate: false);
                Assert.Equal($"https://127.0.0.1:{port}/", response);
            }
        }

        private static int GetWebHostPort(IWebHost webHost)
            => webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses
                .Select(serverAddress => new Uri(serverAddress).Port)
                .Single();
    }
}
