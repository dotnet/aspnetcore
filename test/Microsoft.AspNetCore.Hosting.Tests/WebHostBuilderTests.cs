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
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        [Fact]
        public void Build_honors_UseStartup_with_string()
        {
            var builder = CreateWebHostBuilder().UseServer(new TestServer());

            using (var host = (WebHost)builder.UseStartup("MyStartupAssembly").Build())
            {
                Assert.Equal("MyStartupAssembly", host.Options.ApplicationName);
                Assert.Equal("MyStartupAssembly", host.Options.StartupAssembly);
            }
        }

        [Fact]
        public async Task StartupMissing_Fallback()
        {
            var builder = CreateWebHostBuilder();
            var server = new TestServer();
            using (var host = builder.UseServer(server).UseStartup("MissingStartupAssembly").Build())
            {
                await host.StartAsync();
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
                await host.StartAsync();
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
                await host.StartAsync();
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
                await host.StartAsync();
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
                await host.StartAsync();
                var services = host.Services.GetServices<IApplicationLifetime>();
                Assert.NotNull(services);
                Assert.NotEmpty(services);

                await AssertResponseContains(server.RequestDelegate, "Exception from constructor");
            }
        }

        [Fact]
        public async Task DefaultObjectPoolProvider_IsRegistered()
        {
            var server = new TestServer();
            var host = CreateWebHostBuilder()
                .UseServer(server)
                .Configure(app => { })
                .Build();
            using (host)
            {
                await host.StartAsync();
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
                await host.StartAsync();
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
                await host.StartAsync();
                await AssertResponseContains(server.RequestDelegate, "Exception from Configure");
            }
        }

        [Fact]
        public void DefaultCreatesLoggerFactory()
        {
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.NotNull(host.Services.GetService<ILoggerFactory>());
            }
        }

        [Fact]
        public void ConfigureDefaultServiceProvider()
        {
            var hostBuilder = new WebHostBuilder()
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

            Assert.Throws<InvalidOperationException>(() => hostBuilder.Build().Start());
        }

        [Fact]
        public void ConfigureDefaultServiceProviderWithContext()
        {
            var configurationCallbackCalled = false;
            var hostBuilder = new WebHostBuilder()
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

            Assert.Throws<InvalidOperationException>(() => hostBuilder.Build().Start());
            Assert.True(configurationCallbackCalled);
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

            using (hostBuilder.Build())
            {
                Assert.Equal(2, callCount);
            }
        }

        [Fact]
        public async Task MultipleStartupAssembliesSpecifiedOnlyAddAssemblyOnce()
        {
            var provider = new TestLoggerProvider();
            var assemblyName = "RandomName";
            var data = new Dictionary<string, string>
            {
                { WebHostDefaults.ApplicationKey,  assemblyName },
                { WebHostDefaults.HostingStartupAssembliesKey, assemblyName }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var builder = CreateWebHostBuilder()
                 .UseConfiguration(config)
                 .ConfigureLogging((_, factory) =>
                 {
                     factory.AddProvider(provider);
                 })
                .UseServer(new TestServer());

            // Verify that there was only one exception throw rather than two.
            using (var host = (WebHost)builder.Build())
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
            var hostBuilder = new WebHostBuilder()
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
            var hostBuilder = new WebHostBuilder()
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

        [Fact]
        public void ThereIsAlwaysConfiguration()
        {
            var hostBuilder = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.NotNull(host.Services.GetService<IConfiguration>());
            }
        }

        [Fact]
        public void ConfigureConfigurationSettingsPropagated()
        {
            var hostBuilder = new WebHostBuilder()
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

        [Fact]
        public void CanConfigureConfigurationAndRetrieveFromDI()
        {
            var hostBuilder = new WebHostBuilder()
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

            using (var host = (WebHost)hostBuilder.Build())
            {
                var config = host.Services.GetService<IConfiguration>();
                Assert.NotNull(config);
                Assert.Equal("value1", config["key1"]);
            }
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

            using (var host = hostBuilder.Build())
            {
                Assert.Equal(2, callCount);

                Assert.NotNull(host.Services.GetRequiredService<ServiceA>());
                Assert.NotNull(host.Services.GetRequiredService<ServiceB>());
            }
        }

        [Fact]
        public void CodeBasedSettingsCodeBasedOverride()
        {
            var hostBuilder = new WebHostBuilder()
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvA")
                .UseSetting(WebHostDefaults.EnvironmentKey, "EnvB")
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>();

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.Equal("EnvB", host.Options.Environment);
            }
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

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.Equal("EnvB", host.Options.Environment);
            }
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

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.Equal("EnvB", host.Options.Environment);
            }
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

            using (var host = (WebHost)hostBuilder.Build())
            {
                Assert.Equal("EnvB", host.Options.Environment);
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


            using (var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                Assert.Equal(expected, host.Services.GetService<IHostingEnvironment>().EnvironmentName);
                Assert.Equal(expected, host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().EnvironmentName);
            }
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
            using (var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseEnvironment(expected)
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build()) { }
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

            using (var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseContentRoot("/")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                Assert.Equal("/", host.Services.GetService<IHostingEnvironment>().ContentRootPath);
                Assert.Equal("/", host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().ContentRootPath);
            }
        }

        [Fact]
        public void RelativeContentRootIsResolved()
        {
            using (var host = new WebHostBuilder()
                .UseContentRoot("testroot")
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var basePath = host.Services.GetRequiredService<IHostingEnvironment>().ContentRootPath;
                var basePath2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().ContentRootPath;

                Assert.True(Path.IsPathRooted(basePath));
                Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath);

                Assert.True(Path.IsPathRooted(basePath2));
                Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath2);
            }
        }

        [Fact]
        public void DefaultContentRootIsApplicationBasePath()
        {
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var appBase = AppContext.BaseDirectory;
                Assert.Equal(appBase, host.Services.GetService<IHostingEnvironment>().ContentRootPath);
                Assert.Equal(appBase, host.Services.GetService<Extensions.Hosting.IHostingEnvironment>().ContentRootPath);
            }
        }

        [Fact]
        public void DefaultWebHostBuilderWithNoStartupThrows()
        {
            var host = new WebHostBuilder()
                .UseServer(new TestServer());

            var ex = Assert.Throws<InvalidOperationException>(() => host.Build());

            Assert.Contains("No startup configured.", ex.Message);
        }

        [Fact]
        public void DefaultApplicationNameWithUseStartupOfString()
        {
            var builder = new ConfigurationBuilder();
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup(typeof(Startup).Assembly.GetName().Name)
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostingEnvironment>();
                var hostingEnv2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>();
                Assert.Equal(typeof(Startup).Assembly.GetName().Name, hostingEnv.ApplicationName);
                Assert.Equal(typeof(Startup).Assembly.GetName().Name, hostingEnv2.ApplicationName);
            }
        }

        [Fact]
        public void DefaultApplicationNameWithUseStartupOfT()
        {
            var builder = new ConfigurationBuilder();
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup<StartupNoServices>()
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostingEnvironment>();
                var hostingEnv2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>();
                Assert.Equal(typeof(StartupNoServices).Assembly.GetName().Name, hostingEnv.ApplicationName);
                Assert.Equal(typeof(StartupNoServices).Assembly.GetName().Name, hostingEnv2.ApplicationName);
            }
        }

        [Fact]
        public void DefaultApplicationNameWithUseStartupOfType()
        {
            var builder = new ConfigurationBuilder();
            var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .UseStartup(typeof(StartupNoServices))
                .Build();

            var hostingEnv = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal(typeof(StartupNoServices).Assembly.GetName().Name, hostingEnv.ApplicationName);
        }

        [Fact]
        public void DefaultApplicationNameWithConfigure()
        {
            var builder = new ConfigurationBuilder();
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(app => { })
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostingEnvironment>();

                // Should be the assembly containing this test, because that's where the delegate comes from
                Assert.Equal(typeof(WebHostBuilderTests).Assembly.GetName().Name, hostingEnv.ApplicationName);
            }
        }

        [Fact]
        public void Configure_SupportsNonStaticMethodDelegate()
        {
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(app => { })
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal("Microsoft.AspNetCore.Hosting.Tests", hostingEnv.ApplicationName);
            }
        }

        [Fact]
        public void Configure_SupportsStaticMethodDelegate()
        {
            using (var host = new WebHostBuilder()
                .UseServer(new TestServer())
                .Configure(StaticConfigureMethod)
                .Build())
            {
                var hostingEnv = host.Services.GetService<IHostingEnvironment>();
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

        [Fact]
        public void Build_DoesNotOverrideILoggerFactorySetByConfigureServices()
        {
            var factory = new DisposableLoggerFactory();
            var builder = CreateWebHostBuilder();
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

        [Fact]
        public void Build_RunsHostingStartupAssembliesIfSpecified()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(TestStartupAssembly1.TestHostingStartup1).GetTypeInfo().Assembly.FullName)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                Assert.Equal("1", builder.GetSetting("testhostingstartup1"));
            }
        }

        [Fact]
        public void Build_RunsHostingStartupRunsPrimaryAssemblyFirst()
        {
            var builder = CreateWebHostBuilder()
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

        [Fact]
        public void Build_RunsHostingStartupAssembliesBeforeApplication()
        {
            var startup = new StartupVerifyServiceA();
            var startupAssemblyName = typeof(WebHostBuilderTests).GetTypeInfo().Assembly.GetName().Name;

            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, typeof(WebHostBuilderTests).GetTypeInfo().Assembly.FullName)
                .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IStartup>(startup);
                })
                .UseServer(new TestServer());

            using (var host = builder.Build())
            {
                host.Start();
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

            var host = new WebHostBuilder()
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
        public void Build_HostingStartupAssemblyCanBeExcluded()
        {
            var builder = CreateWebHostBuilder()
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

        [Fact]
        public void Build_ConfigureLoggingInHostingStartupWorks()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .Configure(app =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(nameof(WebHostBuilderTests));
                    logger.LogInformation("From startup");
                })
                .UseServer(new TestServer());

            using (var host = (WebHost)builder.Build())
            {
                host.Start();
                var sink = host.Services.GetRequiredService<ITestSink>();
                Assert.Contains(sink.Writes, w => w.State.ToString() == "From startup");
            }
        }

        [Fact]
        public void Build_ConfigureAppConfigurationInHostingStartupWorks()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = (WebHost)builder.Build())
            {
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                Assert.Equal("value", configuration["testhostingstartup:config"]);
            }
        }

        [Fact]
        public void Build_DoesRunHostingStartupFromPrimaryAssemblyEvenIfNotSpecified()
        {
            var builder = CreateWebHostBuilder()
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Equal("0", builder.GetSetting("testhostingstartup"));
            }
        }

        [Fact]
        public void Build_HostingStartupFromPrimaryAssemblyCanBeDisabled()
        {
            var builder = CreateWebHostBuilder()
                .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Null(builder.GetSetting("testhostingstartup"));
            }
        }

        [Fact]
        public void Build_DoesntThrowIfUnloadableAssemblyNameInHostingStartupAssemblies()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "SomeBogusName")
                .Configure(app => { })
                .UseServer(new TestServer());

            using (builder.Build())
            {
                Assert.Equal("0", builder.GetSetting("testhostingstartup"));
            }
        }

        [Fact]
        public async Task Build_DoesNotThrowIfUnloadableAssemblyNameInHostingStartupAssembliesAndCaptureStartupErrorsTrue()
        {
            var provider = new TestLoggerProvider();
            var builder = CreateWebHostBuilder()
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

        [Fact]
        public void StartupErrorsAreLoggedIfCaptureStartupErrorsIsTrue()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(true)
                .Configure(app =>
                {
                    throw new InvalidOperationException("Startup exception");
                })
                .UseServer(new TestServer());

            using (var host = (WebHost)builder.Build())
            {
                host.Start();
                var sink = host.Services.GetRequiredService<ITestSink>();
                Assert.Contains(sink.Writes, w => w.Exception?.Message == "Startup exception");
            }
        }

        [Fact]
        public void StartupErrorsAreLoggedIfCaptureStartupErrorsIsFalse()
        {
            ITestSink testSink = null;

            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .Configure(app =>
                {
                    testSink = app.ApplicationServices.GetRequiredService<ITestSink>();

                    throw new InvalidOperationException("Startup exception");
                })
                .UseServer(new TestServer());

            Assert.Throws<InvalidOperationException>(() => builder.Build().Start());

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

        [Fact]
        public void UseShutdownTimeoutConfiguresShutdownTimeout()
        {
            var builder = CreateWebHostBuilder()
                .CaptureStartupErrors(false)
                .UseShutdownTimeout(TimeSpan.FromSeconds(102))
                .Configure(app => { })
                .UseServer(new TestServer());

            using (var host = (WebHost)builder.Build())
            {
                Assert.Equal(TimeSpan.FromSeconds(102), host.Options.ShutdownTimeout);
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

        internal class StartupVerifyServiceA : IStartup
        {
            internal ServiceA ServiceA { get; set; }

            internal ServiceDescriptor ServiceADescriptor { get; set; }

            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                ServiceADescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ServiceA));

                return services.BuildServiceProvider();
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
