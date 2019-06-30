// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Xunit;

[assembly: HostingStartup(typeof(WebHostBuilderTests.TestHostingStartup))]

namespace Microsoft.AspNetCore.Hosting
{
    public class WebHostBuilderTests
    {
        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_honors_UseStartup_with_string(IWebHostBuilder builder)
        {
            builder = builder.UseServer(new TestServer());

            using (var host = builder.UseStartup("MyStartupAssembly").Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal("MyStartupAssembly", options.ApplicationName);
                Assert.Equal("MyStartupAssembly", options.StartupAssembly);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupMissing_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            using (var host = builder.UseServer(server).UseStartup("MissingStartupAssembly").Build())
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "MissingStartupAssembly");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupStaticCtorThrows_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupStaticCtorThrows>().Build();
            using (host)
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Exception from static constructor");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupCtorThrows_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (host)
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupCtorThrows_TypeLoadException(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupThrowTypeLoadException>().Build();
            using (host)
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Message from the LoaderException</div>");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task IHostApplicationLifetimeRegisteredEvenWhenStartupCtorThrows_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupCtorThrows>().Build();
            using (host)
            {
                await host.StartAsync();
                var services = host.Services.GetServices<IHostApplicationLifetime>();
                Assert.NotNull(services);
                Assert.NotEmpty(services);

                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupConfigureServicesThrows_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (host)
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Exception from ConfigureServices");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task StartupConfigureThrows_Fallback(IWebHostBuilder builder)
        {
            var server = new TestServer();
            var host = builder.UseServer(server).UseStartup<StartupConfigureServicesThrows>().Build();
            using (host)
            {
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Exception from Configure");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void DefaultCreatesLoggerFactory(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                Assert.NotNull(host.Services.GetService<ILoggerFactory>());
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void ConfigureDefaultServiceProvider(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseServer(new TestServer())
                .ConfigureServices(s =>
                {
                    s.AddTransient<ServiceD>();
                    s.AddScoped<ServiceC>();
                })
                .Configure(app =>
                {
                    app.ApplicationServices.GetRequiredService<ServiceC>();
                })
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateScopes = true;
                });

            using var host = hostBuilder.Build();
            Assert.Throws<InvalidOperationException>(() => host.Start());
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void ConfigureDefaultServiceProviderWithContext(IWebHostBuilder builder)
        {
            var configurationCallbackCalled = false;
            var hostBuilder = builder
                .UseServer(new TestServer())
                .ConfigureServices(s =>
                {
                    s.AddTransient<ServiceD>();
                    s.AddScoped<ServiceC>();
                })
                .Configure(app =>
                {
                    app.ApplicationServices.GetRequiredService<ServiceC>();
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    Assert.NotNull(context.HostingEnvironment);
                    Assert.NotNull(context.Configuration);
                    configurationCallbackCalled = true;
                    options.ValidateScopes = true;
                });

            using  var host = hostBuilder.Build();
            Assert.Throws<InvalidOperationException>(() => host.Start());
            Assert.True(configurationCallbackCalled);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void MultipleConfigureLoggingInvokedInOrder(IWebHostBuilder builder)
        {
            var callCount = 0; //Verify ordering
            var hostBuilder = builder
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

            using (hostBuilder.Build())
            {
                Assert.Equal(2, callCount);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task MultipleStartupAssembliesSpecifiedOnlyAddAssemblyOnce(IWebHostBuilder builder)
        {
            var provider = new TestLoggerProvider();
            var assemblyName = "RandomName";
            var data = new Dictionary<string, string>
            {
                { WebHostDefaults.ApplicationKey,  assemblyName },
                { WebHostDefaults.HostingStartupAssembliesKey, assemblyName }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            builder = builder
                 .UseConfiguration(config)
                 .ConfigureLogging((_, factory) =>
                 {
                     factory.AddProvider(provider);
                 })
                .UseServer(new TestServer());

            // Verify that there was only one exception throw rather than two.
            using (var host = builder.Build())
            {
                await host.StartAsync();
                var context = provider.Sink.Writes.Where(s => s.EventId.Id == LoggerEventIds.HostingStartupAssemblyException);
                Assert.NotNull(context);
                Assert.Single(context);
            }
        }

        [Fact]
        public void HostingContextContainsAppConfigurationDuringConfigureLogging()
        {
            var hostBuilder = CreateWebHostBuilder()
                 .ConfigureAppConfiguration((context, configBuilder) =>
                    configBuilder.AddInMemoryCollection(
                        new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", "value1")
                        }))
                 .ConfigureLogging((context, factory) =>
                 {
                     Assert.Equal("value1", context.Configuration["key1"]);
                 })
                 .UseServer(new TestServer())
                 .UseStartup<StartupNoServices>();

            using (hostBuilder.Build()) { }
        }

        [Fact]
        public void HostingContextContainsAppConfigurationDuringConfigureServices()
        {
            var hostBuilder = CreateWebHostBuilder()
                 .ConfigureAppConfiguration((context, configBuilder) =>
                    configBuilder.AddInMemoryCollection(
                        new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", "value1")
                        }))
                 .ConfigureServices((context, factory) =>
                 {
                     Assert.Equal("value1", context.Configuration["key1"]);
                 })
                 .UseServer(new TestServer())
                 .UseStartup<StartupNoServices>();

            using (hostBuilder.Build()) { }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void ThereIsAlwaysConfiguration(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                Assert.NotNull(host.Services.GetService<IConfiguration>());
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void ConfigureConfigurationSettingsPropagated(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseSetting("key1", "value1")
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    var config = configBuilder.Build();
                    Assert.Equal("value1", config["key1"]);
                })
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (hostBuilder.Build()) { }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void CanConfigureConfigurationAndRetrieveFromDI(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder
                        .AddInMemoryCollection(
                            new KeyValuePair<string, string>[]
                            {
                                new KeyValuePair<string, string>("key1", "value1")
                            })
                        .AddEnvironmentVariables();
                })
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                var config = host.Services.GetService<IConfiguration>();
                Assert.NotNull(config);
                Assert.Equal("value1", config["key1"]);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DoNotCaptureStartupErrorsByDefault(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseServer(new TestServer())
                .UseStartup<StartupBoom>();

            var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
            Assert.Equal("A public method named 'ConfigureProduction' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupBoom' type.", exception.Message);
        }

        [Fact]
        public void ServiceProviderDisposedOnBuildException()
        {
            var service = new DisposableService();
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .ConfigureServices(services =>
                {
                    // Added as a factory since instances are never disposed by the container
                    services.AddSingleton(sp => service);
                })
                .UseStartup<StartupWithResolvedDisposableThatThrows>();

            Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
            Assert.True(service.Disposed);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void CaptureStartupErrorsHonored(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .CaptureStartupErrors(false)
                .UseServer(new TestServer())
                .UseStartup<StartupBoom>();

            var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
            Assert.Equal("A public method named 'ConfigureProduction' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupBoom' type.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void ConfigureServices_CanBeCalledMultipleTimes(IWebHostBuilder builder)
        {
            var callCount = 0; // Verify ordering
            var hostBuilder = builder
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

            using (var host = hostBuilder.Build())
            {
                Assert.Equal(2, callCount);

                Assert.NotNull(host.Services.GetRequiredService<ServiceA>());
                Assert.NotNull(host.Services.GetRequiredService<ServiceB>());
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void CodeBasedSettingsCodeBasedOverride(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvA")
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvB")
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal("EnvB", options.Environment);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void CodeBasedSettingsConfigBasedOverride(IWebHostBuilder builder)
        {
            var settings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvB" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var hostBuilder = builder
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvA")
                .UseConfiguration(config)
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal("EnvB", options.Environment);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void ConfigBasedSettingsCodeBasedOverride(IWebHostBuilder builder)
        {
            var settings = new Dictionary<string, string>
            {
                { WebHostDefaults.EnvironmentKey, "EnvA" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var hostBuilder = builder
                .UseConfiguration(config)
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvB")
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal("EnvB", options.Environment);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void ConfigBasedSettingsConfigBasedOverride(IWebHostBuilder builder)
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

            var hostBuilder = builder
                .UseConfiguration(config)
                .UseConfiguration(overrideConfig)
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = hostBuilder.Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal("EnvB", options.Environment);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void UseEnvironmentIsNotOverriden(IWebHostBuilder builder)
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = configBuilder.Build();

            var expected = "MY_TEST_ENVIRONMENT";


            using (var host = builder
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                Assert.Equal(expected, host.Services.GetService<IHostEnvironment>().EnvironmentName);
                Assert.Equal(expected, host.Services.GetService<IWebHostEnvironment>().EnvironmentName);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal(expected, host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>().EnvironmentName);
                Assert.Equal(expected, host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().EnvironmentName);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void BuildAndDispose(IWebHostBuilder builder)
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = configBuilder.Build();

            var expected = "MY_TEST_ENVIRONMENT";
            using (var host = builder
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build()) { }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void UseBasePathConfiguresBasePath(IWebHostBuilder builder)
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = configBuilder.Build();

            using (var host = builder
                .UseConfiguration(config)
                .UseContentRoot("/")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                Assert.Equal("/", host.Services.GetService<IHostEnvironment>().ContentRootPath);
                Assert.Equal("/", host.Services.GetService<IWebHostEnvironment>().ContentRootPath);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal("/", host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>().ContentRootPath);
                Assert.Equal("/", host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().ContentRootPath);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void RelativeContentRootIsResolved(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseContentRoot("testroot")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var basePath = host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath;
#pragma warning disable CS0618 // Type or member is obsolete
                var basePath2 = host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>().ContentRootPath;
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.True(Path.IsPathRooted(basePath));
                Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath);

                Assert.True(Path.IsPathRooted(basePath2));
                Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath2);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultContentRootIsApplicationBasePath(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var appBase = AppContext.BaseDirectory;
                Assert.Equal(appBase, host.Services.GetService<IHostEnvironment>().ContentRootPath);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal(appBase, host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>().ContentRootPath);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultWebHostBuilderWithNoStartupThrows(IWebHostBuilder builder)
        {
            builder.UseServer(new TestServer());

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                using var host = builder.Build();
                host.Start();
            });

            Assert.Contains("No application configured.", ex.Message);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultApplicationNameWithUseStartupOfString(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .UseStartup(typeof(Startup).Assembly.GetName().Name)
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostEnvironment>();
#pragma warning disable CS0618 // Type or member is obsolete
                var hostingEnv2 = host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(typeof(Startup).Assembly.GetName().Name, hostingEnv.ApplicationName);
                Assert.Equal(typeof(Startup).Assembly.GetName().Name, hostingEnv2.ApplicationName);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultApplicationNameWithUseStartupOfT(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .UseStartup<StartupNoServicesNoInterface>()
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostEnvironment>();
#pragma warning disable CS0618 // Type or member is obsolete
                var hostingEnv2 = host.Services.GetService<AspNetCore.Hosting.IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(typeof(StartupNoServicesNoInterface).Assembly.GetName().Name, hostingEnv.ApplicationName);
                Assert.Equal(typeof(StartupNoServicesNoInterface).Assembly.GetName().Name, hostingEnv2.ApplicationName);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultApplicationNameWithUseStartupOfType(IWebHostBuilder builder)
        {
            using var host = builder
                .UseServer(new TestServer())
                .UseStartup(typeof(StartupNoServicesNoInterface))
                .Build();

            var hostingEnv = host.Services.GetService<IHostEnvironment>();
            Assert.Equal(typeof(StartupNoServicesNoInterface).Assembly.GetName().Name, hostingEnv.ApplicationName);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void DefaultApplicationNameWithConfigure(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .Configure(app => { })
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostEnvironment>();

                // Should be the assembly containing this test, because that's where the delegate comes from
                Assert.Equal(typeof(WebHostBuilderTests).Assembly.GetName().Name, hostingEnv.ApplicationName);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void Configure_SupportsNonStaticMethodDelegate(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .Configure(app => { })
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostEnvironment>();
                Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public void Configure_SupportsStaticMethodDelegate(IWebHostBuilder builder)
        {
            using (var host = builder
                .UseServer(new TestServer())
                .Configure(StaticConfigureMethod)
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostEnvironment>();
                Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
            }
        }

        [Fact]
        public void Build_DoesNotAllowBuildingMuiltipleTimes()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            using (builder.UseServer(server)
                .UseStartup<StartupNoServices>()
                .Build())
            {
                var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
                Assert.Equal("WebHostBuilder allows creation only of a single instance of WebHost", ex.Message);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_DoesNotOverrideILoggerFactorySetByConfigureServices(IWebHostBuilder builder)
        {
            var factory = new DisposableLoggerFactory();
            var server = new TestServer();

            using (var host = builder.UseServer(server)
                .ConfigureServices(collection => collection.AddSingleton<ILoggerFactory>(factory))
                .UseStartup<StartupWithILoggerFactory>()
                .Build())
            {
                var factoryFromHost = host.Services.GetService<ILoggerFactory>();
                Assert.Equal(factory, factoryFromHost);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_RunsHostingStartupAssembliesIfSpecified(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(TestStartupAssembly1.TestHostingStartup1).GetTypeInfo().Assembly.FullName)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                Assert.Equal("1", builder.GetSetting("testhostingstartup1"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_RunsHostingStartupRunsPrimaryAssemblyFirst(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(TestStartupAssembly1.TestHostingStartup1).GetTypeInfo().Assembly.FullName)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                Assert.Equal("0", builder.GetSetting("testhostingstartup"));
                Assert.Equal("1", builder.GetSetting("testhostingstartup1"));
                Assert.Equal("01", builder.GetSetting("testhostingstartup_chain"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_RunsHostingStartupAssembliesBeforeApplication(IWebHostBuilder builder)
        {
            var startupAssemblyName = typeof(WebHostBuilderTests).GetTypeInfo().Assembly.GetName().Name;

            builder = builder
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(WebHostBuilderTests).GetTypeInfo().Assembly.FullName)
                .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
                .UseStartup<StartupVerifyServiceA>()
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                host.Start();
                var startup = host.Services.GetRequiredService<StartupVerifyServiceA>();
                Assert.NotNull(startup.ServiceADescriptor);
                Assert.NotNull(startup.ServiceA);
            }
        }

        [Fact]
        public async Task ExternalContainerInstanceCanBeUsedForEverything()
        {
            var disposables = new List<DisposableService>();

            var containerFactory = new ExternalContainerFactory(services =>
            {
                services.AddSingleton(sp =>
                {
                    var service = new DisposableService();
                    disposables.Add(service);
                    return service;
                });
            });

            var host = CreateWebHostBuilder()
                .UseStartup<StartupWithExternalServices>()
                .UseServer(new TestServer())
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IServiceProviderFactory<IServiceCollection>>(containerFactory);
                })
                .Build();

            using (host)
            {
                await host.StartAsync();
            }

            // We should create the hosting service provider and the application service provider
            Assert.Equal(2, containerFactory.ServiceProviders.Count);
            Assert.Equal(2, disposables.Count);

            Assert.NotEqual(disposables[0], disposables[1]);
            Assert.True(disposables[0].Disposed);
            Assert.True(disposables[1].Disposed);
        }

        [Fact]
        public void GenericWebHostThrowsWithIStartup()
        {
            var builder = new GenericWebHostBuilderWrapper(new HostBuilder())
                .UseStartup<StartupNoServices>();

            var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
            Assert.Equal("Microsoft.AspNetCore.Hosting.IStartup isn't supported", exception.Message);
        }

        [Fact]
        public void GenericWebHostThrowsOnBuild()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
            {
                var hostBuilder = new HostBuilder()
                       .ConfigureWebHost(builder =>
                       {
                           builder.UseStartup<StartupNoServices>();
                           builder.Build();
                       });
            });

            Assert.Equal("Building this implementation of IWebHostBuilder is not supported.", exception.Message);
        }

        [Fact]
        public void GenericWebHostDoesNotSupportBuildingInConfigureServices()
        {
            var hostBuilder = new HostBuilder()
                   .ConfigureWebHost(builder =>
                   {
                       builder.UseStartup<StartupWithBuiltConfigureServices>();
                   });
            var exception = Assert.Throws<NotSupportedException>(() =>
            {
                hostBuilder.Build();
            });

            Assert.Equal($"ConfigureServices returning an {typeof(IServiceProvider)} isn't supported.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_HostingStartupAssemblyCanBeExcluded(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(TestStartupAssembly1.TestHostingStartup1).GetTypeInfo().Assembly.FullName)
                .UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, typeof(TestStartupAssembly1.TestHostingStartup1).GetTypeInfo().Assembly.FullName)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                Assert.Null(builder.GetSetting("testhostingstartup1"));
                Assert.Equal("0", builder.GetSetting("testhostingstartup_chain"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_ConfigureLoggingInHostingStartupWorks(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .Configure(app =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(nameof(WebHostBuilderTests));
                    logger.LogInformation("From startup");
                })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                host.Start();
                var sink = host.Services.GetRequiredService<ITestSink>();
                Assert.Contains(sink.Writes, w => w.State.ToString() == "From startup");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_ConfigureAppConfigurationInHostingStartupWorks(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                Assert.Equal("value", configuration["testhostingstartup:config"]);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_AppConfigAvailableEverywhere(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(
                        new[]
                        {
                            new KeyValuePair<string,string>("appconfig", "appvalue")
                        });
                })
                .ConfigureLogging((context, logging) =>
                {
                    Assert.Equal("appvalue", context.Configuration["appconfig"]);
                })
                .ConfigureServices((context, services) =>
                {
                    Assert.Equal("appvalue", context.Configuration["appconfig"]);
                })
                .UseDefaultServiceProvider((context, services) =>
                {
                    Assert.Equal("appvalue", context.Configuration["appconfig"]);
                })
                .UseStartup<StartupCheckConfig>()
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                Assert.Equal("appvalue", configuration["appconfig"]);
            }
        }

        public class StartupCheckConfig
        {
            public StartupCheckConfig(IConfiguration config)
            {
                Assert.Equal("value", config["testhostingstartup:config"]);
            }

            public void Configure(IApplicationBuilder app)
            {

            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_DoesRunHostingStartupFromPrimaryAssemblyEvenIfNotSpecified(IWebHostBuilder builder)
        {
            builder = builder
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Equal("0", builder.GetSetting("testhostingstartup"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_HostingStartupFromPrimaryAssemblyCanBeDisabled(IWebHostBuilder builder)
        {
            builder = builder
                .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Null(builder.GetSetting("testhostingstartup"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void Build_DoesntThrowIfUnloadableAssemblyNameInHostingStartupAssemblies(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "SomeBogusName")
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Equal("0", builder.GetSetting("testhostingstartup"));
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public async Task Build_DoesNotThrowIfUnloadableAssemblyNameInHostingStartupAssembliesAndCaptureStartupErrorsTrue(IWebHostBuilder builder)
        {
            var provider = new TestLoggerProvider();
            builder = builder
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddProvider(provider);
                })
                .CaptureStartupErrors(true)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "SomeBogusName")
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                await host.StartAsync();
                var context = provider.Sink.Writes.FirstOrDefault(s => s.EventId.Id == LoggerEventIds.HostingStartupAssemblyException);
                Assert.NotNull(context);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void StartupErrorsAreLoggedIfCaptureStartupErrorsIsTrue(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(true)
                .Configure(app =>
                {
                    throw new InvalidOperationException("Startup exception");
                })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                host.Start();
                var sink = host.Services.GetRequiredService<ITestSink>();
                Assert.Contains(sink.Writes, w => w.Exception?.Message == "Startup exception");
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void StartupErrorsAreLoggedIfCaptureStartupErrorsIsFalse(IWebHostBuilder builder)
        {
            ITestSink testSink = null;

            builder = builder
                .CaptureStartupErrors(false)
                .Configure(app =>
                {
                    testSink = app.ApplicationServices.GetRequiredService<ITestSink>();

                    throw new InvalidOperationException("Startup exception");
                })
                .UseServer(new TestServer());

            using var host = builder.Build();
            Assert.Throws<InvalidOperationException>(() => host.Start());

            Assert.NotNull(testSink);
            Assert.Contains(testSink.Writes, w => w.Exception?.Message == "Startup exception");
        }

        [Fact]
        public void HostingStartupTypeCtorThrowsIfNull()
        {
            Assert.Throws<ArgumentNullException>(() => new HostingStartupAttribute(null));
        }

        [Fact]
        public void HostingStartupTypeCtorThrowsIfNotIHosting()
        {
            Assert.Throws<ArgumentException>(() => new HostingStartupAttribute(typeof(WebHostTests)));
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void UseShutdownTimeoutConfiguresShutdownTimeout(IWebHostBuilder builder)
        {
            builder = builder
                .CaptureStartupErrors(false)
                .UseShutdownTimeout(TimeSpan.FromSeconds(102))
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                var options = new WebHostOptions(host.Services.GetRequiredService<IConfiguration>());
                Assert.Equal(TimeSpan.FromSeconds(102), options.ShutdownTimeout);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public async Task StartupFiltersDoNotRunIfNotApplicationConfigured(IWebHostBuilder builder)
        {
            var hostBuilder = builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter, MyStartupFilter>();
                })
                .UseServer(new TestServer());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using var host = hostBuilder.Build();

                var filter = (MyStartupFilter)host.Services.GetServices<IStartupFilter>().FirstOrDefault(s => s is MyStartupFilter);
                Assert.NotNull(filter);
                try
                {
                    await host.StartAsync();
                }
                finally
                {
                    Assert.False(filter.Executed);
                }
            });

            Assert.Contains("No application configured.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuildersWithConfig))]
        public void UseConfigurationWithSectionAddsSubKeys(IWebHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("key", "value"),
                    new KeyValuePair<string, string>("nested:key", "nestedvalue"),
                }).Build();
            var section = config.GetSection("nested");

            builder = builder
                .CaptureStartupErrors(false)
                .Configure(app => { })
                .UseConfiguration(section)
                .UseServer(new TestServer());

            Assert.Equal("nestedvalue", builder.GetSetting("key"));

            using  var host = builder.Build();
            var appConfig = host.Services.GetRequiredService<IConfiguration>();
            Assert.Equal("nestedvalue", appConfig["key"]);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public async Task ThrowingFromHostedServiceFailsStartAsync(IWebHostBuilder builder)
        {
            builder.Configure(app => { })
                   .ConfigureServices(services =>
                   {
                       services.AddHostedService<ThrowingHostedService>();
                   })
                   .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                var startEx = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
                Assert.Equal("Hosted Service throws in StartAsync", startEx.Message);
                var stopEx = await Assert.ThrowsAsync<AggregateException>(() => host.StopAsync());
                Assert.Single(stopEx.InnerExceptions);
                Assert.Equal("Hosted Service throws in StopAsync", stopEx.InnerExceptions[0].Message);
            }
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public async Task ThrowingFromHostedServiceStopsOtherHostedServicesFromRunningStartAsync(IWebHostBuilder builder)
        {
            builder.Configure(app => { })
                   .ConfigureServices(services =>
                   {
                       services.AddHostedService<ThrowingHostedService>();
                       services.AddHostedService<NonThrowingHostedService>();
                   })
                   .UseServer(new TestServer());

            using var host = builder.Build();
            var service = host.Services.GetServices<IHostedService>().OfType<NonThrowingHostedService>().First();
            var startEx = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
            Assert.Equal("Hosted Service throws in StartAsync", startEx.Message);

            var stopEx = await Assert.ThrowsAsync<AggregateException>(() => host.StopAsync());
            Assert.Single(stopEx.InnerExceptions);
            Assert.Equal("Hosted Service throws in StopAsync", stopEx.InnerExceptions[0].Message);

            // This service is never constructed
            Assert.False(service.StartCalled);
            Assert.True(service.StopCalled);
        }

        [Theory]
        [MemberData(nameof(DefaultWebHostBuilders))]
        public async Task HostedServicesStartedBeforeServer(IWebHostBuilder builder)
        {
            builder.Configure(app => { })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<StartOrder>();
                    services.AddHostedService<MustBeStartedFirst>();
                    services.AddSingleton<IServer, ServerMustBeStartedSecond>();
                });

            using var host = builder.Build();
            await host.StartAsync();
            var ordering = host.Services.GetRequiredService<StartOrder>();
            Assert.Equal(2, ordering.Order);
            await host.StopAsync();
        }

        private class StartOrder
        {
            public int Order { get; set; }
        }

        private class MustBeStartedFirst : IHostedService
        {
            public MustBeStartedFirst(StartOrder ordering)
            {
                Ordering = ordering;
            }

            public StartOrder Ordering { get; }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Assert.Equal(0, Ordering.Order);
                Ordering.Order++;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private class ServerMustBeStartedSecond : IServer
        {
            public ServerMustBeStartedSecond(StartOrder ordering)
            {
                Ordering = ordering;
            }

            public StartOrder Ordering { get; }

            public IFeatureCollection Features => null;

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            {
                Assert.Equal(1, Ordering.Order);
                Ordering.Order++;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        private static void StaticConfigureMethod(IApplicationBuilder app) { }

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

        public static TheoryData<IWebHostBuilder> DefaultWebHostBuilders => new TheoryData<IWebHostBuilder>
        {
            new WebHostBuilder(),
            new GenericWebHostBuilderWrapper(new HostBuilder())
        };

        public static TheoryData<IWebHostBuilder> DefaultWebHostBuildersWithConfig
        {
            get
            {
                var vals = new Dictionary<string, string>
                {
                    { "DetailedErrors", "true" },
                    { "captureStartupErrors", "true" }
                };

                var builder = new ConfigurationBuilder()
                    .AddInMemoryCollection(vals);
                var config = builder.Build();

                return new TheoryData<IWebHostBuilder> {
                    new WebHostBuilder().UseConfiguration(config),
                    new GenericWebHostBuilderWrapper(new HostBuilder()).UseConfiguration(config)
                };
            }
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

        private class ThrowingHostedService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("Hosted Service throws in StartAsync");
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException("Hosted Service throws in StopAsync");
            }
        }

        private class NonThrowingHostedService : IHostedService
        {
            public bool StartCalled { get; set; }
            public bool StopCalled { get; set; }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                StartCalled = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                StopCalled = true;
                return Task.CompletedTask;
            }
        }

        private class MyStartupFilter : IStartupFilter
        {
            public bool Executed { get; set; }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                Executed = true;
                return next;
            }
        }

        private class TestServer : IServer
        {
            IFeatureCollection IServer.Features { get; }
            public RequestDelegate RequestDelegate { get; private set; }

            public void Dispose() { }

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
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

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        internal class ExternalContainerFactory : IServiceProviderFactory<IServiceCollection>
        {
            private readonly Action<IServiceCollection> _configureServices;
            private readonly List<IServiceProvider> _serviceProviders = new List<IServiceProvider>();

            public List<IServiceProvider> ServiceProviders => _serviceProviders;

            public ExternalContainerFactory(Action<IServiceCollection> configureServices)
            {
                _configureServices = configureServices;
            }

            public IServiceCollection CreateBuilder(IServiceCollection services)
            {
                _configureServices(services);
                return services;
            }

            public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
            {
                var provider = containerBuilder.BuildServiceProvider();
                _serviceProviders.Add(provider);
                return provider;
            }
        }

        internal class StartupWithExternalServices
        {
            public DisposableService DisposableServiceCtor { get; set; }

            public DisposableService DisposableServiceApp { get; set; }

            public StartupWithExternalServices(DisposableService disposable)
            {
                DisposableServiceCtor = disposable;
            }

            public void ConfigureServices(IServiceCollection services) { }

            public void Configure(IApplicationBuilder app, DisposableService disposable)
            {
                DisposableServiceApp = disposable;
            }
        }

        internal class StartupVerifyServiceA
        {
            internal ServiceA ServiceA { get; set; }

            internal ServiceDescriptor ServiceADescriptor { get; set; }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton(this);

                ServiceADescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ServiceA));
            }

            public void Configure(IApplicationBuilder app)
            {
                ServiceA = app.ApplicationServices.GetService<ServiceA>();
            }
        }

        public class DisposableService : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public class TestHostingStartup : IHostingStartup
        {
            public void Configure(IWebHostBuilder builder)
            {
                var loggerProvider = new TestLoggerProvider();
                builder.UseSetting("testhostingstartup", "0")
                       .UseSetting("testhostingstartup_chain", builder.GetSetting("testhostingstartup_chain") + "0")
                       .ConfigureServices(services =>
                       {
                           // This check is required because MVC still uses the
                           // IWebHostEnvironment instance before the container is baked
#pragma warning disable CS0618 // Type or member is obsolete
                           var heDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IHostingEnvironment));
                           Assert.NotNull(heDescriptor);
                           Assert.NotNull(heDescriptor.ImplementationInstance);
#pragma warning restore CS0618 // Type or member is obsolete
                           var wheDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IWebHostEnvironment));
                           Assert.NotNull(wheDescriptor);
                           Assert.NotNull(wheDescriptor.ImplementationInstance);
                       })
                       .ConfigureServices(services => services.AddSingleton<ServiceA>())
                       .ConfigureServices(services => services.AddSingleton<ITestSink>(loggerProvider.Sink))
                       .ConfigureLogging((_, lf) => lf.AddProvider(loggerProvider))
                       .ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.AddInMemoryCollection(
                           new[]
                           {
                               new KeyValuePair<string,string>("testhostingstartup:config", "value")
                           }));
            }
        }

        public class StartupWithResolvedDisposableThatThrows
        {
            public StartupWithResolvedDisposableThatThrows(DisposableService service)
            {

            }

            public void ConfigureServices(IServiceCollection services)
            {
                throw new InvalidOperationException();
            }

            public void Configure(IApplicationBuilder app)
            {

            }
        }

        public class TestLoggerProvider : ILoggerProvider
        {
            public TestSink Sink { get; set; } = new TestSink();

            public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, Sink, enabled: true);

            public void Dispose() { }
        }

        private class ServiceC
        {
            public ServiceC(ServiceD serviceD) { }
        }

        internal class ServiceD { }

        internal class ServiceA { }

        internal class ServiceB { }

        private class DisposableLoggerFactory : ILoggerFactory
        {
            public void Dispose()
            {
                Disposed = true;
            }

            public bool Disposed { get; set; }

            public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

            public void AddProvider(ILoggerProvider provider) { }
        }
    }
}
