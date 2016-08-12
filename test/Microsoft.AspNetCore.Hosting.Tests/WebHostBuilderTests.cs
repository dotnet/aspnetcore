// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public class WebHostBuilderTests
    {
        [Fact]
        public void Build_honors_UseStartup_with_string()
        {
            var builder = CreateWebHostBuilder().UseServer(new TestServer());

            var host = (WebHost)builder.UseStartup("MyStartupAssembly").Build();

            Assert.Equal("MyStartupAssembly", host.Options.ApplicationName);
            Assert.Equal("MyStartupAssembly", host.Options.StartupAssembly);
        }

        [Fact]
        public async Task StartupMissing_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup("MissingStartupAssembly").Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "MissingStartupAssembly");
            }
        }

        [Fact]
        public async Task StartupStaticCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupStaticCtorThrows>().Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from static constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task StartupCtorThrows_TypeLoadException()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupThrowTypeLoadException>().Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "Message from the LoaderException</div>");
            }
        }

        [Fact]
        public async Task IApplicationLifetimeRegisteredEvenWhenStartupCtorThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (host)
            {
                host.Start();
                var services = host.Services.GetServices<IApplicationLifetime>();
                Assert.NotNull(services);
                Assert.NotEmpty(services);

                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public void DefaultObjectPoolProvider_IsRegistered()
        {
            var server = new TestServer();
            var host = CreateWebHostBuilder()
                .UseServer(server)
                .Configure(app => { })
                .Build();
            using (host)
            {
                host.Start();
                Assert.IsType<DefaultObjectPoolProvider>(host.Services.GetService<ObjectPoolProvider>());
            }
        }

        [Fact]
        public async Task StartupConfigureServicesThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from ConfigureServices");
            }
        }

        [Fact]
        public async Task StartupConfigureThrows_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (host)
            {
                host.Start();
                await AssertResponseContains(server.RequestDelegate, "Exception from Configure");
            }
        }

        [Fact]
        public void DefaultCreatesLoggerFactory()
        {
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.NotNull(host.Services.GetService<ILoggerFactory>());
        }

        [Fact]
        public void UseLoggerFactoryHonored()
        {
            var loggerFactory = new LoggerFactory();

            var hostBuilder = new WebHostBuilder()
                .UseLoggerFactory(loggerFactory)
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.Same(loggerFactory, host.Services.GetService<ILoggerFactory>());
        }

        [Fact]
        public void MultipleConfigureLoggingInvokedInOrder()
        {
            var callCount = 0; //Verify ordering
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging(loggerFactory =>
                {
                    Assert.Equal(0, callCount++);
                })
                .ConfigureLogging(loggerFactory =>
                {
                    Assert.Equal(1, callCount++);
                })
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void DoNotCaptureStartupErrorsByDefault()
        {
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupBoom>();

            var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
            Assert.Equal("A public method named 'ConfigureProduction' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupBoom' type.", exception.Message);
        }

        [Fact]
        public void CaptureStartupErrorsHonored()
        {
            var hostBuilder = new WebHostBuilder()
                .CaptureStartupErrors(false)
                .UseServer(new TestServer())
                .UseStartup<StartupBoom>();

            var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
            Assert.Equal("A public method named 'ConfigureProduction' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupBoom' type.", exception.Message);
        }

        [Fact]
        public void ConfigureServices_CanBeCalledMultipleTimes()
        {
            var callCount = 0; // Verify ordering
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .ConfigureServices(services =>
                {
                    Assert.Equal(0, callCount++);
                    services.AddTransient<ServiceA>();
                })
                .ConfigureServices(services =>
                {
                    Assert.Equal(1, callCount++);
                    services.AddTransient<ServiceB>();
                })
                .Configure(app => { });

            var host = hostBuilder.Build();
            Assert.Equal(2, callCount);

            Assert.NotNull(host.Services.GetRequiredService<ServiceA>());
            Assert.NotNull(host.Services.GetRequiredService<ServiceB>());
        }

        [Fact]
        public void CodeBasedSettingsCodeBasedOverride()
        {
            var hostBuilder = new WebHostBuilder()
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvA")
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvB")
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.Equal("EnvB", host.Options.Environment);
        }

        [Fact]
        public void CodeBasedSettingsConfigBasedOverride()
        {
            var settings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvB" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var hostBuilder = new WebHostBuilder()
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvA")
                .UseConfiguration(config)
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.Equal("EnvB", host.Options.Environment);
        }

        [Fact]
        public void ConfigBasedSettingsCodeBasedOverride()
        {
            var settings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvA" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var hostBuilder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvB")
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.Equal("EnvB", host.Options.Environment);
        }

        [Fact]
        public void ConfigBasedSettingsConfigBasedOverride()
        {
            var settings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvA" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var overrideSettings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvB" }
            };

            var overrideConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(overrideSettings)
                .Build();

            var hostBuilder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseConfiguration(overrideConfig)
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            var host = (WebHost)hostBuilder.Build();

            Assert.Equal("EnvB", host.Options.Environment);
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
            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            Assert.Equal(expected, host.Services.GetService<IHostingEnvironment>().EnvironmentName);
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
            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            host.Dispose();
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

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseContentRoot("/")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            Assert.Equal("/", host.Services.GetService<IHostingEnvironment>().ContentRootPath);
        }

        [Fact]
        public void RelativeContentRootIsResolved()
        {
            var contentRootNet451 = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                "testroot" : "../../../../test/Microsoft.AspNetCore.Hosting.Tests/testroot";

            var host = new WebHostBuilder()
#if NET451
                .UseContentRoot(contentRootNet451)
#else
                .UseContentRoot("testroot")
#endif
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            var basePath = host.Services.GetRequiredService<IHostingEnvironment>().ContentRootPath;
            Assert.True(Path.IsPathRooted(basePath));
            Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath);
        }

        [Fact]
        public void DefaultContentRootIsApplicationBasePath()
        {
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            var appBase = PlatformServices.Default.Application.ApplicationBasePath;
            Assert.Equal(appBase, host.Services.GetService<IHostingEnvironment>().ContentRootPath);
        }

        [Fact]
        public void DefaultApplicationNameToStartupAssemblyName()
        {
            var builder = new ConfigurationBuilder();
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
        }

        [Fact]
        public void DefaultApplicationNameToStartupType()
        {
            var builder = new ConfigurationBuilder();
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests.NonExistent")
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Microsoft.AspNetCore.Hosting.Tests.NonExistent", hostingEnv.ApplicationName);
        }

        [Fact]
        public void DefaultApplicationNameAndBasePathToStartupMethods()
        {
            var builder = new ConfigurationBuilder();
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(app => { })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests.NonExistent")
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Microsoft.AspNetCore.Hosting.Tests.NonExistent", hostingEnv.ApplicationName);
        }

        [Fact]
        public void Configure_SupportsNonStaticMethodDelegate()
        {
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(app => { })
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
        }

        [Fact]
        public void Configure_SupportsStaticMethodDelegate()
        {
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(StaticConfigureMethod)
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
        }

        private static void StaticConfigureMethod(IApplicationBuilder app)
        { }

        private IWebHostBuilder CreateWebHostBuilder()
        {
            var vals = new Dictionary<string, string>
            {
                { "DetailedErrors", "true" },
                { "captureStartupErrors", "true" }
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            return new WebHostBuilder().UseConfiguration(config);
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

        private class ServiceA
        {

        }

        private class ServiceB
        {

        }
    }
}
