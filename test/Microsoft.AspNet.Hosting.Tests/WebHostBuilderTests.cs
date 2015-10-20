// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilderTests
    {
        [Fact]
        public void Build_uses_application_for_startup_assembly_by_default()
        {
            var builder = CreateWebHostBuilder();

            var engine = (HostingEngine)builder.Build();

            Assert.Equal("Microsoft.AspNet.Hosting.Tests", engine.StartupAssemblyName);
        }

        [Fact]
        public void Build_honors_UseStartup_with_string()
        {
            var builder = CreateWebHostBuilder();

            var engine = (HostingEngine)builder.UseStartup("MyStartupAssembly").Build();

            Assert.Equal("MyStartupAssembly", engine.StartupAssemblyName);
        }

        [Fact]
        public async Task StartupMissing_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup("MissingStartupAssembly").Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "MissingStartupAssembly");
            }
        }

        [Fact]
        public async Task StartupStaticCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupStaticCtorThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "Exception from static constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_TypeLoadException()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupThrowTypeLoadException>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "Message from the LoaderException</span>");
            }
        }

        [Fact]
        public async Task IApplicationLifetimeRegisteredEvenWhenStartupCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (engine.Start())
            {
                var service = engine.ApplicationServices.GetServices<IApplicationLifetime>();
                Assert.NotNull(service);
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupConfigureServicesThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "Exception from ConfigureServices");
            }
        }

        [Fact]
        public async Task StartupConfigureThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var engine = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(server.RequestDelegate, "Exception from Configure");
            }
        }

        private WebHostBuilder CreateWebHostBuilder()
        {
            var vals = new Dictionary<string, string>
            {
                { "server", "Microsoft.AspNet.Hosting.Tests" },
                { "Hosting:DetailedErrors", "true" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            return new WebHostBuilder(config, captureStartupErrors: true);
        }

        private async Task AssertResponseContains(RequestDelegate app, string expectedText)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            await app(httpContext);
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var bodyText = new StreamReader(httpContext.Response.Body).ReadToEnd();
            Assert.Contains(expectedText, bodyText);
        }

        private class TestServer : IServer
        {
            IFeatureCollection IServer.Features { get; }
            public RequestDelegate RequestDelegate { get; private set; }

            public void Dispose()
            {

            }

            public void Start(RequestDelegate requestDelegate)
            {
                RequestDelegate = requestDelegate;
            }
        }
    }
}
