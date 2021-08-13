// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class WebApplicationTests
    {
        [Fact]
        public async Task WebApplicationBuilderConfiguration_IncludesCommandLineArguments()
        {
            var builder = WebApplication.CreateBuilder(new string[] { "--urls", "http://localhost:5001" });
            Assert.Equal("http://localhost:5001", builder.Configuration["urls"]);

            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            await app.StartAsync();

            var address = Assert.Single(urls);
            Assert.Equal("http://localhost:5001", address);

            Assert.Same(app.Urls, urls);

            var url = Assert.Single(urls);
            Assert.Equal("http://localhost:5001", url);
        }

        [Fact]
        public async Task WebApplicationRunAsync_UsesDefaultUrls()
        {
            var builder = WebApplication.CreateBuilder();
            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            await app.StartAsync();

            Assert.Same(app.Urls, urls);

            Assert.Equal(2, urls.Count);
            Assert.Equal("http://localhost:5000", urls[0]);
            Assert.Equal("https://localhost:5001", urls[1]);
        }

        [Fact]
        public async Task WebApplicationRunUrls_UpdatesIServerAddressesFeature()
        {
            var builder = WebApplication.CreateBuilder();
            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            var runTask = app.RunAsync("http://localhost:5001");

            var url = Assert.Single(urls);
            Assert.Equal("http://localhost:5001", url);

            await app.StopAsync();
            await runTask;
        }

        [Fact]
        public async Task WebApplicationUrls_UpdatesIServerAddressesFeature()
        {
            var builder = WebApplication.CreateBuilder();
            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            app.Urls.Add("http://localhost:5002");
            app.Urls.Add("https://localhost:5003");

            await app.StartAsync();

            Assert.Equal(2, urls.Count);
            Assert.Equal("http://localhost:5002", urls[0]);
            Assert.Equal("https://localhost:5003", urls[1]);
        }

        [Fact]
        public async Task WebApplicationRunUrls_OverridesIServerAddressesFeature()
        {
            var builder = WebApplication.CreateBuilder();
            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            app.Urls.Add("http://localhost:5002");
            app.Urls.Add("https://localhost:5003");

            var runTask = app.RunAsync("http://localhost:5001");

            var url = Assert.Single(urls);
            Assert.Equal("http://localhost:5001", url);

            await app.StopAsync();
            await runTask;
        }

        [Fact]
        public async Task WebApplicationUrls_ThrowsInvalidOperationExceptionIfThereIsNoIServerAddressesFeature()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IServer>(new MockAddressesServer());
            await using var app = builder.Build();

            Assert.Throws<InvalidOperationException>(() => app.Urls);
        }

        [Fact]
        public async Task WebApplicationRunUrls_ThrowsInvalidOperationExceptionIfThereIsNoIServerAddressesFeature()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IServer>(new MockAddressesServer());
            await using var app = builder.Build();

            await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync("http://localhost:5001"));
        }

        [Fact]
        public async Task WebApplicationRunUrls_ThrowsInvalidOperationExceptionIfServerAddressesFeatureIsReadOnly()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IServer>(new MockAddressesServer(new List<string>().AsReadOnly()));
            await using var app = builder.Build();

            await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync("http://localhost:5001"));
        }

        [Fact]
        public void WebApplicationBuilderHost_ThrowsWhenBuiltDirectly()
        {
            Assert.Throws<NotSupportedException>(() => ((IHostBuilder)WebApplication.CreateBuilder().Host).Build());
        }

        [Fact]
        public void WebApplicationBuilderWebHost_ThrowsWhenBuiltDirectly()
        {
            Assert.Throws<NotSupportedException>(() => ((IWebHostBuilder)WebApplication.CreateBuilder().WebHost).Build());
        }

        [Fact]
        public void WebApplicationBuilderWebHostSettingsThatAffectTheHostCannotBeModified()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ApplicationKey, nameof(WebApplicationTests)));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.EnvironmentKey, envName));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ContentRootKey, contentRoot));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "hosting"));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, "hostingexclude"));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseEnvironment(envName));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseContentRoot(contentRoot));
        }

        [Fact]
        public void WebApplicationBuilderWebHostSettingsThatAffectTheHostCannotBeModifiedViaConfigureAppConfiguration()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.ApplicationKey, nameof(WebApplicationTests) }
                });
            }));

            Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.EnvironmentKey, envName }
                });
            }));

            Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.ContentRootKey, contentRoot }
                });
            }));

            Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.HostingStartupAssembliesKey, "hosting" }
                });
            }));

            Assert.Throws<NotSupportedException>(() => builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.HostingStartupExcludeAssembliesKey, "hostingexclude" }
                });
            }));
        }

        [Fact]
        public void WebApplicationBuilderSettingInvalidApplicationWillFailAssemblyLoadForUserSecrets()
        {
            var options = new WebApplicationOptions
            {
                ApplicationName = nameof(WebApplicationTests), // This is not a real assembly
                EnvironmentName = Environments.Development
            };

            // Use secrets fails to load an invalid assembly name
            Assert.Throws<FileNotFoundException>(() => WebApplication.CreateBuilder(options).Build());
        }

        [Fact]
        public void WebApplicationBuilderCanConfigureHostSettingsUsingWebApplicationOptions()
        {
            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            var options = new WebApplicationOptions
            {
                ApplicationName = nameof(WebApplicationTests),
                ContentRootPath = contentRoot,
                EnvironmentName = envName
            };

            var builder = new WebApplicationBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
        }

        [Fact]
        public void WebApplicationBuilderWebApplicationOptionsPropertiesOverridesArgs()
        {
            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            var options = new WebApplicationOptions
            {
                Args = new[] {
                    $"--{WebHostDefaults.ApplicationKey}=testhost",
                    $"--{WebHostDefaults.ContentRootKey}=c:\foo",
                    $"--{WebHostDefaults.EnvironmentKey}=Test"
                },
                ApplicationName = nameof(WebApplicationTests),
                ContentRootPath = contentRoot,
                EnvironmentName = envName,
            };

            var builder = new WebApplicationBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
        }

        [Fact]
        public void WebApplicationBuilderCanConfigureHostSettingsUsingWebApplicationOptionsArgs()
        {
            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            var options = new WebApplicationOptions
            {
                Args = new[] {
                    $"--{WebHostDefaults.ApplicationKey}={nameof(WebApplicationTests)}",
                    $"--{WebHostDefaults.ContentRootKey}={contentRoot}",
                    $"--{WebHostDefaults.EnvironmentKey}={envName}"
                }
            };

            var builder = new WebApplicationBuilder(
                options,
                bootstrapBuilder =>
                {
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                        Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                        Assert.Equal(contentRoot, context.HostingEnvironment.ContentRootPath);
                    });
                });

            Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
            Assert.Equal(envName, builder.Environment.EnvironmentName);
            Assert.Equal(contentRoot, builder.Environment.ContentRootPath);
        }

        [Fact]
        public void WebApplicationBuilderApplicationNameDefaultsToEntryAssembly()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;

            var builder = new WebApplicationBuilder(
                new(),
                bootstrapBuilder =>
                {
                    // Verify the defaults observed by the boostrap host builder we use internally to populate
                    // the defaults
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
                    });
                });

            Assert.Equal(assemblyName, builder.Environment.ApplicationName);
            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
            });

            builder.WebHost.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
            });

            var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
            var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal(assemblyName, hostEnv.ApplicationName);
            Assert.Equal(assemblyName, webHostEnv.ApplicationName);
        }

        [Fact]
        public void WebApplicationBuilderApplicationNameCanBeOverridden()
        {
            var assemblyName = typeof(WebApplicationTests).Assembly.GetName().Name;

            var options = new WebApplicationOptions
            {
                ApplicationName = assemblyName
            };

            var builder = new WebApplicationBuilder(
                options,
                bootstrapBuilder =>
                {
                    // Verify the defaults observed by the boostrap host builder we use internally to populate
                    // the defaults
                    bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
                    });
                });

            Assert.Equal(assemblyName, builder.Environment.ApplicationName);
            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
            });

            builder.WebHost.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal(assemblyName, context.HostingEnvironment.ApplicationName);
            });

            var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
            var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal(assemblyName, hostEnv.ApplicationName);
            Assert.Equal(assemblyName, webHostEnv.ApplicationName);
        }

        [Fact]
        public void WebApplicationBuilderCanFlowCommandLineConfigurationToApplication()
        {
            var builder = WebApplication.CreateBuilder(new[] { "--x=1", "--name=Larry", "--age=20", "--environment=Testing" });

            Assert.Equal("1", builder.Configuration["x"]);
            Assert.Equal("Larry", builder.Configuration["name"]);
            Assert.Equal("20", builder.Configuration["age"]);
            Assert.Equal("Testing", builder.Configuration["environment"]);
            Assert.Equal("Testing", builder.Environment.EnvironmentName);

            builder.WebHost.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal("Testing", context.HostingEnvironment.EnvironmentName);
            });

            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                Assert.Equal("Testing", context.HostingEnvironment.EnvironmentName);
            });

            var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
            var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal("Testing", hostEnv.EnvironmentName);
            Assert.Equal("Testing", webHostEnv.EnvironmentName);
            Assert.Equal("1", app.Configuration["x"]);
            Assert.Equal("Larry", app.Configuration["name"]);
            Assert.Equal("20", app.Configuration["age"]);
            Assert.Equal("Testing", app.Configuration["environment"]);
        }

        [Fact]
        public void WebApplicationBuilderHostBuilderSettingsThatAffectTheHostCannotBeModified()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureHostConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { HostDefaults.ApplicationKey, "myapp" }
                });
            }));
            Assert.Throws<NotSupportedException>(() => builder.Host.UseEnvironment(envName));
            Assert.Throws<NotSupportedException>(() => builder.Host.UseContentRoot(contentRoot));
        }

        [Fact]
        public void WebApplicationBuilderCanModifyWebRootAfterCreateBuilder()
        {
            var builder = WebApplication.CreateBuilder();

            var webRoot = Path.GetTempPath().ToString();

            builder.WebHost.UseSetting("WEBROOT", webRoot);

            Assert.Equal(webRoot, builder.WebHost.GetSetting("webroot"));

            var app = builder.Build();

            Assert.Equal(webRoot, app.Environment.WebRootPath);
        }

        [Fact]
        public void WebApplicationBuilderWebRootIsRelativeToContentRoot()
        {
            var contentRoot = Path.GetTempPath().ToString();

            var builder = WebApplication.CreateBuilder(new[] { $"--contentRoot={contentRoot}" });

            builder.WebHost.UseSetting("WEBROOT", "wwwroot");

            Assert.Equal("wwwroot", builder.WebHost.GetSetting("webroot"));

            var app = builder.Build();

            Assert.Equal(Path.Combine(contentRoot, "wwwroot"), app.Environment.WebRootPath);
        }

        [Fact]
        public void WebApplicationBuilderWebHostUseSettingCanBeReadByConfiguration()
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseSetting("A", "value");
            builder.WebHost.UseSetting("B", "another");

            Assert.Equal("value", builder.WebHost.GetSetting("A"));
            Assert.Equal("another", builder.WebHost.GetSetting("B"));

            var app = builder.Build();

            Assert.Equal("value", app.Configuration["A"]);
            Assert.Equal("another", app.Configuration["B"]);

            Assert.Equal("value", builder.Configuration["A"]);
            Assert.Equal("another", builder.Configuration["B"]);
        }

        [Fact]
        public async Task WebApplicationCanObserveConfigurationChangesMadeInBuild()
        {
            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>()
                    {
                        { "A", "A" },
                        { "B", "B" },
                    });
                });

                hostBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>()
                    {
                        { "C", "C" },
                        { "D", "D" },
                    });
                });

                hostBuilder.ConfigureWebHost(builder =>
                {
                    builder.UseSetting("E", "E");

                    builder.ConfigureAppConfiguration(config =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string>()
                        {
                            { "F", "F" },
                        });
                    });
                });
            });

            var builder = WebApplication.CreateBuilder();

            await using var app = builder.Build();

            Assert.Equal("A", app.Configuration["A"]);
            Assert.Equal("B", app.Configuration["B"]);
            Assert.Equal("C", app.Configuration["C"]);
            Assert.Equal("D", app.Configuration["D"]);
            Assert.Equal("E", app.Configuration["E"]);
            Assert.Equal("F", app.Configuration["F"]);

            Assert.Equal("A", builder.Configuration["A"]);
            Assert.Equal("B", builder.Configuration["B"]);
            Assert.Equal("C", builder.Configuration["C"]);
            Assert.Equal("D", builder.Configuration["D"]);
            Assert.Equal("E", builder.Configuration["E"]);
            Assert.Equal("F", builder.Configuration["F"]);
        }

        [Fact]
        public void WebApplicationBuilderHostProperties_IsCaseSensitive()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Host.Properties["lowercase"] = nameof(WebApplicationTests);

            Assert.Equal(nameof(WebApplicationTests), builder.Host.Properties["lowercase"]);
            Assert.False(builder.Host.Properties.ContainsKey("Lowercase"));
        }

        [Fact]
        public async Task WebApplicationConfiguration_HostFilterOptionsAreReloadable()
        {
            var builder = WebApplication.CreateBuilder();
            var host = builder.WebHost
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.Add(new ReloadableMemorySource());
                });
            await using var app = builder.Build();

            var config = app.Services.GetRequiredService<IConfiguration>();
            var monitor = app.Services.GetRequiredService<IOptionsMonitor<HostFilteringOptions>>();
            var options = monitor.CurrentValue;

            Assert.Contains("*", options.AllowedHosts);

            var changed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            monitor.OnChange(newOptions =>
            {
                changed.TrySetResult(0);
            });

            config["AllowedHosts"] = "NewHost";

            await changed.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            options = monitor.CurrentValue;
            Assert.Contains("NewHost", options.AllowedHosts);
        }

        [Fact]
        public async Task WebApplicationConfiguration_EnablesForwardedHeadersFromConfig()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration["FORWARDEDHEADERS_ENABLED"] = "true";
            await using var app = builder.Build();

            app.Run(context =>
            {
                Assert.Equal("https", context.Request.Scheme);
                return Task.CompletedTask;
            });

            await app.StartAsync();

            var client = app.GetTestClient();
            client.DefaultRequestHeaders.Add("x-forwarded-proto", "https");
            var result = await client.GetAsync("http://localhost/");
            result.EnsureSuccessStatusCode();
        }

        [Fact]
        public void WebApplicationCreate_RegistersRouting()
        {
            var app = WebApplication.Create();
            var linkGenerator = app.Services.GetService(typeof(LinkGenerator));
            Assert.NotNull(linkGenerator);
        }

        [Fact]
        public void WebApplication_CanResolveDefaultServicesFromServiceCollection()
        {
            var builder = WebApplication.CreateBuilder();

            // Add the service collection to the service collection
            builder.Services.AddSingleton(builder.Services);

            var app = builder.Build();

            var env0 = app.Services.GetRequiredService<IHostEnvironment>();

            var env1 = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider().GetRequiredService<IHostEnvironment>();

            Assert.Equal(env0.ApplicationName, env1.ApplicationName);
            Assert.Equal(env0.EnvironmentName, env1.EnvironmentName);
            Assert.Equal(env0.ContentRootPath, env1.ContentRootPath);
        }

        [Fact]
        public async Task WebApplication_CanResolveServicesAddedAfterBuildFromServiceCollection()
        {
            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IService, Service>();
                });
            });

            var builder = WebApplication.CreateBuilder();

            // Add the service collection to the service collection
            builder.Services.AddSingleton(builder.Services);

            await using var app = builder.Build();

            var service0 = app.Services.GetRequiredService<IService>();

            var service1 = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider().GetRequiredService<IService>();

            Assert.IsType<Service>(service0);
            Assert.IsType<Service>(service1);
        }

        [Fact]
        public void WebApplication_CanResolveDefaultServicesFromServiceCollectionInCorrectOrder()
        {
            var builder = WebApplication.CreateBuilder();

            // Add the service collection to the service collection
            builder.Services.AddSingleton(builder.Services);

            // We're overriding the default IHostLifetime so that we can test the order in which it's resolved.
            // This should override the default IHostLifetime.
            builder.Services.AddSingleton<IHostLifetime, CustomHostLifetime>();

            var app = builder.Build();

            var hostLifetime0 = app.Services.GetRequiredService<IHostLifetime>();
            var childServiceProvider = app.Services.GetRequiredService<IServiceCollection>().BuildServiceProvider();
            var hostLifetime1 = childServiceProvider.GetRequiredService<IHostLifetime>();

            var hostLifetimes0 = app.Services.GetServices<IHostLifetime>().ToArray();
            var hostLifetimes1 = childServiceProvider.GetServices<IHostLifetime>().ToArray();

            Assert.IsType<CustomHostLifetime>(hostLifetime0);
            Assert.IsType<CustomHostLifetime>(hostLifetime1);

            Assert.Equal(hostLifetimes1.Length, hostLifetimes0.Length);
        }

        [Fact]
        public async Task WebApplication_CanCallUseRoutingWithoutUseEndpoints()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/new", () => "new");

            // Rewrite "/old" to "/new" before matching routes
            app.Use((context, next) =>
            {
                if (context.Request.Path == "/old")
                {
                    context.Request.Path = "/new";
                }

                return next(context);
            });

            app.UseRouting();

            await app.StartAsync();

            var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();

            var newEndpoint = Assert.Single(endpointDataSource.Endpoints);
            var newRouteEndpoint = Assert.IsType<RouteEndpoint>(newEndpoint);
            Assert.Equal("/new", newRouteEndpoint.RoutePattern.RawText);

            var client = app.GetTestClient();

            var oldResult = await client.GetAsync("http://localhost/old");
            oldResult.EnsureSuccessStatusCode();

            Assert.Equal("new", await oldResult.Content.ReadAsStringAsync());
        }

        // REVIEW: David doesn't like this behavior :)
        [Fact]
        public async Task WebApplication_CanCallUseEndpointsWithoutUseRouting()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/1", () => "1");

            app.UseEndpoints(endpoints => { });

            await app.StartAsync();

            var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();

            var newEndpoint = Assert.Single(endpointDataSource.Endpoints);
            var newRouteEndpoint = Assert.IsType<RouteEndpoint>(newEndpoint);
            Assert.Equal("/1", newRouteEndpoint.RoutePattern.RawText);

            var client = app.GetTestClient();

            var oldResult = await client.GetAsync("http://localhost/1");
            oldResult.EnsureSuccessStatusCode();

            Assert.Equal("1", await oldResult.Content.ReadAsStringAsync());
        }

        [Fact]
        public void WebApplicationCreate_RegistersEventSourceLogger()
        {
            var listener = new TestEventListener();
            var app = WebApplication.Create();

            var logger = app.Services.GetRequiredService<ILogger<WebApplicationTests>>();
            var guid = Guid.NewGuid().ToString();
            logger.LogInformation(guid);

            var events = listener.EventData.ToArray();
            Assert.Contains(events, args =>
                args.EventSource.Name == "Microsoft-Extensions-Logging" &&
                args.Payload.OfType<string>().Any(p => p.Contains(guid)));
        }

        [Fact]
        public void WebApplicationBuilder_CanClearDefaultLoggers()
        {
            var listener = new TestEventListener();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<WebApplicationTests>>();
            var guid = Guid.NewGuid().ToString();
            logger.LogInformation(guid);

            var events = listener.EventData.ToArray();
            Assert.DoesNotContain(events, args =>
                args.EventSource.Name == "Microsoft-Extensions-Logging" &&
                args.Payload.OfType<string>().Any(p => p.Contains(guid)));
        }

        [Fact]
        public void WebApplicationBuilder_CanSetWebRootPaths()
        {
            var builder = WebApplication.CreateBuilder();
            var webRootPath = "www";
            var fullWebRootPath = Path.Combine(Directory.GetCurrentDirectory(), webRootPath);

            builder.WebHost.UseWebRoot(webRootPath);
            Assert.Equal(webRootPath, builder.WebHost.GetSetting("webroot"));

            var app = builder.Build();
            Assert.Equal(fullWebRootPath, app.Environment.WebRootPath);
        }

        [Fact]
        public void WebApplicationBuilder_CanChangeSetWebRootPathsViaConfigureAppConfiguration()
        {
            var builder = WebApplication.CreateBuilder();
            var webRootPath = "www";
            var fullWebRootPath = Path.Combine(Directory.GetCurrentDirectory(), webRootPath);

            builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { WebHostDefaults.WebRootKey , webRootPath }
                });
            });

            Assert.Equal(webRootPath, builder.WebHost.GetSetting("webroot"));

            var app = builder.Build();
            Assert.Equal(fullWebRootPath, app.Environment.WebRootPath);
        }

        [Fact]
        public async Task WebApplicationBuilder_StartupFilterCanAddTerminalMiddleware()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<IStartupFilter, TerminalMiddlewareStartupFilter>();
            await using var app = builder.Build();

            app.MapGet("/defined", () => { });

            await app.StartAsync();

            var client = app.GetTestClient();

            var definedResult = await client.GetAsync("http://localhost/defined");
            definedResult.EnsureSuccessStatusCode();

            var terminalResult = await client.GetAsync("http://localhost/undefined");
            Assert.Equal(418, (int)terminalResult.StatusCode);
        }

        [Fact]
        public async Task StartupFilter_WithUseRoutingWorks()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<IStartupFilter, MyFilter>();
            await using var app = builder.Build();

            var chosenEndpoint = string.Empty;
            app.MapGet("/", async c => {
                chosenEndpoint = c.GetEndpoint().DisplayName;
                await c.Response.WriteAsync("Hello World");
            }).WithDisplayName("One");

            await app.StartAsync();

            var client = app.GetTestClient();

            _ = await client.GetAsync("http://localhost/");
            Assert.Equal("One", chosenEndpoint);

            var response = await client.GetAsync("http://localhost/1");
            Assert.Equal(203, ((int)response.StatusCode));
        }

        class MyFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseRouting();
                    next(app);
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/1", async c =>
                        {
                            c.Response.StatusCode = 203;
                            await c.Response.WriteAsync("Hello Filter");
                        }).WithDisplayName("Two");
                    });
                };
            }
        }

        [Fact]
        public async Task WebApplicationBuilder_OnlyAddsDefaultServicesOnce()
        {
            var builder = WebApplication.CreateBuilder();

            // IWebHostEnvironment is added by ConfigureDefaults
            Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<LoggerFactoryOptions>)));
            // IWebHostEnvironment is added by ConfigureWebHostDefaults
            Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IWebHostEnvironment)));
            Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IOptionsChangeTokenSource<HostFilteringOptions>)));
            Assert.Single(builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IServer)));

            await using var app = builder.Build();

            Assert.Single(app.Services.GetRequiredService<IEnumerable<IConfigureOptions<LoggerFactoryOptions>>>());
            Assert.Single(app.Services.GetRequiredService<IEnumerable<IWebHostEnvironment>>());
            Assert.Single(app.Services.GetRequiredService<IEnumerable<IOptionsChangeTokenSource<HostFilteringOptions>>>());
            Assert.Single(app.Services.GetRequiredService<IEnumerable<IServer>>());
        }

        [Fact]
        public void WebApplicationBuilder_EnablesServiceScopeValidationByDefaultInDevelopment()
        {
            // The environment cannot be reconfigured after the builder is created currently.
            var builder = WebApplication.CreateBuilder(new[] { "--environment", "Development" });

            builder.Services.AddScoped<Service>();
            builder.Services.AddSingleton<Service2>();

            // This currently throws an AggregateException, but any Exception from Build() is enough to make this test pass.
            // If this is throwing for any reason other than service scope validation, we'll likely see it in other tests.
            Assert.ThrowsAny<Exception>(() => builder.Build());
        }

        [Fact]
        public async Task WebApplicationBuilder_ThrowsExceptionIfServicesAlreadyBuilt()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            Assert.Throws<InvalidOperationException>(() => builder.Services.AddSingleton<IService>(new Service()));
            Assert.Throws<InvalidOperationException>(() => builder.Services.TryAddSingleton(new Service()));
            Assert.Throws<InvalidOperationException>(() => builder.Services.AddScoped<IService, Service>());
            Assert.Throws<InvalidOperationException>(() => builder.Services.TryAddScoped<IService, Service>());
            Assert.Throws<InvalidOperationException>(() => builder.Services.Remove(ServiceDescriptor.Singleton(new Service())));
            Assert.Throws<InvalidOperationException>(() => builder.Services[0] = ServiceDescriptor.Singleton(new Service()));
        }

        [Fact]
        public void WebApplicationBuilder_ThrowsFromExtensionMethodsNotSupportedByHostAndWebHost()
        {
            var builder = WebApplication.CreateBuilder();

            Assert.Throws<NotSupportedException>(() => builder.WebHost.Configure(app => { }));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup<MyStartup>());
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup(typeof(MyStartup)));

            Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }));
            Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }, options => { }));
            Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHostDefaults(webHostBuilder => { }));
        }

        [Fact]
        public async Task EndpointDataSourceOnlyAddsOnce()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            app.UseRouting();

            app.MapGet("/", () => "Hello World!").WithDisplayName("One");

            app.UseEndpoints(routes =>
            {
                routes.MapGet("/hi", () => "Hi World").WithDisplayName("Two");
                routes.MapGet("/heyo", () => "Heyo World").WithDisplayName("Three");
            });

            app.Start();

            var ds = app.Services.GetRequiredService<EndpointDataSource>();
            Assert.Equal(3, ds.Endpoints.Count);
            Assert.Equal("One", ds.Endpoints[0].DisplayName);
            Assert.Equal("Two", ds.Endpoints[1].DisplayName);
            Assert.Equal("Three", ds.Endpoints[2].DisplayName);
        }

        [Fact]
        public async Task RoutesAddedToCorrectMatcher()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.UseRouting();

            var chosenRoute = string.Empty;

            app.Use((context, next) =>
            {
                chosenRoute = context.GetEndpoint()?.DisplayName;
                return next(context);
            });

            app.MapGet("/", () => "Hello World").WithDisplayName("One");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/hi", () => "Hello Endpoints").WithDisplayName("Two");
            });

            app.UseRouting();
            app.UseEndpoints(_ => { });

            await app.StartAsync();

            var client = app.GetTestClient();

            _ = await client.GetAsync("http://localhost/");
            Assert.Equal("One", chosenRoute);
        }

        [Fact]
        public async Task AppPropertiesDoNotContainARouteBuilder()
        {
            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            app.UseRouting();

            app.UseEndpoints(endpoints => { });

            app.Start();

            Assert.False(app.Properties.TryGetValue("__EndpointRouteBuilder", out _));
            Assert.False(app.Properties.TryGetValue("__GlobalEndpointRouteBuilder", out _));
        }

        private class Service : IService { }
        private interface IService { }

        private class Service2
        {
            public Service2(Service service)
            {
            }
        }

        private class MyStartup : IStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                throw new NotImplementedException();
            }

            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class HostingListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
        {
            private readonly Action<IHostBuilder> _configure;
            private static readonly AsyncLocal<HostingListener> _currentListener = new();
            private readonly IDisposable _subscription0;
            private IDisposable _subscription1;

            public HostingListener(Action<IHostBuilder> configure)
            {
                _configure = configure;

                _subscription0 = DiagnosticListener.AllListeners.Subscribe(this);

                _currentListener.Value = this;
            }

            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(DiagnosticListener value)
            {
                if (_currentListener.Value != this)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Name == "Microsoft.Extensions.Hosting")
                {
                    _subscription1 = value.Subscribe(this);
                }
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                if (value.Key == "HostBuilding")
                {
                    _configure?.Invoke((IHostBuilder)value.Value);
                }
            }

            public void Dispose()
            {
                // Undo this here just in case the code unwinds synchronously since that doesn't revert
                // the execution context to the original state. Only async methods do that on exit.
                _currentListener.Value = null;

                _subscription0.Dispose();
                _subscription1?.Dispose();
            }
        }

        private class CustomHostLifetime : IHostLifetime
        {
            public Task StopAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task WaitForStartAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;

            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-Extensions-Logging")
                {
                    EnableEvents(eventSource, EventLevel.Informational);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (!_disposed)
                {
                    _events.Enqueue(eventData);
                }
            }

            public override void Dispose()
            {
                _disposed = true;
                base.Dispose();
            }
        }

        private class ReloadableMemorySource : IConfigurationSource
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return new ReloadableMemoryProvider();
            }
        }

        private class ReloadableMemoryProvider : ConfigurationProvider
        {
            public override void Set(string key, string value)
            {
                base.Set(key, value);
                OnReload();
            }
        }

        private class MockAddressesServer : IServer
        {
            private readonly ICollection<string> _urls;

            public MockAddressesServer()
            {
                // For testing a server that doesn't set an IServerAddressesFeature.
            }

            public MockAddressesServer(ICollection<string> urls)
            {
                _urls = urls;

                var mockAddressesFeature = new MockServerAddressesFeature
                {
                    Addresses = urls
                };

                Features.Set<IServerAddressesFeature>(mockAddressesFeature);
            }

            public IFeatureCollection Features { get; } = new FeatureCollection();

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
            {
                if (_urls.Count == 0)
                {
                    // This is basically Kestrel's DefaultAddressStrategy.
                    _urls.Add("http://localhost:5000");
                    _urls.Add("https://localhost:5001");
                }

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }

            private class MockServerAddressesFeature : IServerAddressesFeature
            {
                public ICollection<string> Addresses { get; set; }
                public bool PreferHostingUrls { get; set; }
            }
        }

        private class TerminalMiddlewareStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);
                    app.Run(context =>
                    {
                        context.Response.StatusCode = 418; // I'm a teapot
                        return Task.CompletedTask;
                    });
                };
            }
        }
    }
}
