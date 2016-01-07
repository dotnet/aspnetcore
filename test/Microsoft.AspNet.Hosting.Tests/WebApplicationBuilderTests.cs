// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class WebApplicationBuilderTests
    {
        [Fact]
        public void Build_honors_UseStartup_with_string()
        {
            var builder = CreateWebApplicationBuilder().UseServer(new TestServer());

            var application = (WebApplication)builder.UseStartup("MyStartupAssembly").Build();

            Assert.Equal("MyStartupAssembly", application.StartupAssemblyName);
        }

        [Fact]
        public async Task StartupMissing_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup("MissingStartupAssembly").Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "MissingStartupAssembly");
            }
        }

        [Fact]
        public async Task StartupStaticCtorThrows_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupStaticCtorThrows>().Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from static constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_TypeLoadException()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupThrowTypeLoadException>().Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "Message from the LoaderException</span>");
            }
        }

        [Fact]
        public async Task IApplicationLifetimeRegisteredEvenWhenStartupCtorThrows_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (application)
            {
                application.Start();
                var service = application.Services.GetServices<IApplicationLifetime>();
                Assert.NotNull(service);
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupConfigureServicesThrows_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from ConfigureServices");
            }
        }

        [Fact]
        public async Task StartupConfigureThrows_Fallback()
        {
            var builder = CreateWebApplicationBuilder();
            var server = new TestServer();
            var application = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (application)
            {
                application.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from Configure");
            }
        }

        [Fact]
        public void UseEnvironmentIsNotOverriden()
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var expected = "MY_TEST_ENVIRONMENT";
            var application = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build();

            Assert.Equal(expected, application.Services.GetService<IHostingEnvironment>().EnvironmentName);
        }

        [Fact]
        public void BuildAndDispose()
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var expected = "MY_TEST_ENVIRONMENT";
            var application = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build();

            application.Dispose();
        }

        [Fact]
        public void UseBasePathConfiguresBasePath()
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var application = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseApplicationBasePath("/foo/bar")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build();

            Assert.Equal("/foo/bar", application.Services.GetService<IApplicationEnvironment>().ApplicationBasePath);
        }

        private IWebApplicationBuilder CreateWebApplicationBuilder()
        {
            var vals = new Dictionary<string, string>
            {
                { "DetailedErrors", "true" },
                { "captureStartupErrors", "true" }
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            return new WebApplicationBuilder().UseConfiguration(config);
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

            public void Start<TContext>(IHttpApplication<TContext> application)
            {
                RequestDelegate = async ctx =>
                {
                    var httpContext = application.CreateContext(ctx.Features);
                    try
                    {
                        await application.ProcessRequestAsync(httpContext);
                    }
                    catch (Exception ex)
                    {
                        application.DisposeContext(httpContext, ex);
                        throw;
                    }
                    application.DisposeContext(httpContext, null);
                };
            }
        }
    }
}
