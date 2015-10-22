// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Configuration;
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
            var serverFactory = new TestServerFactory();
            var engine = (HostingEngine)builder.UseServer(serverFactory).UseStartup("MissingStartupAssembly").Build();
            using (engine.Start())
            {
                await AssertResponseContains(serverFactory.Application, "MissingStartupAssembly");
            }
        }

        [Fact]
        public async Task StartupStaticCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var serverFactory = new TestServerFactory();
            var engine = (HostingEngine)builder.UseServer(serverFactory).UseStartup<StartupStaticCtorThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(serverFactory.Application, "Exception from static constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var serverFactory = new TestServerFactory();
            var engine = (HostingEngine)builder.UseServer(serverFactory).UseStartup<StartupCtorThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(serverFactory.Application, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupConfigureServicesThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var serverFactory = new TestServerFactory();
            var engine = (HostingEngine)builder.UseServer(serverFactory).UseStartup<StartupConfigureServicesThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(serverFactory.Application, "Exception from ConfigureServices");
            }
        }

        [Fact]
        public async Task StartupConfigureThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var serverFactory = new TestServerFactory();
            var engine = (HostingEngine)builder.UseServer(serverFactory).UseStartup<StartupConfigureServicesThrows>().Build();
            using (engine.Start())
            {
                await AssertResponseContains(serverFactory.Application, "Exception from Configure");
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

        private async Task AssertResponseContains(Func<IFeatureCollection, Task> app, string expectedText)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            await app(httpContext.Features);
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var bodyText = new StreamReader(httpContext.Response.Body).ReadToEnd();
            Assert.Contains(expectedText, bodyText);
        }

        private class TestServerFactory : IServerFactory
        {
            public Func<IFeatureCollection, Task> Application { get; set; }

            public IFeatureCollection Initialize(IConfiguration configuration)
            {
                return new FeatureCollection();
            }

            public IDisposable Start(IFeatureCollection serverFeatures, Func<IFeatureCollection, Task> application)
            {
                Application = application;
                return new Disposable();
            }

            private class Disposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
