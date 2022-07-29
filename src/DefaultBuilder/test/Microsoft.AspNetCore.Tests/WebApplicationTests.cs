// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Reflection;
using System.Text;
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
using Microsoft.AspNetCore.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: HostingStartup(typeof(WebApplicationTests.TestHostingStartup))]

namespace Microsoft.AspNetCore.Tests
{
    public class WebApplicationTests
    {
        [Fact]
        public async Task WebApplicationBuilder_New()
        {
            var builder = WebApplication.CreateBuilder(new string[] { "--urls", "http://localhost:5001" });

            await using var app = builder.Build();
            var newApp = (app as IApplicationBuilder).New();
            Assert.NotNull(newApp.ServerFeatures);
        }

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
        public async Task WebApplicationWebHostUseUrls_OverridesDefaultHostingConfiguration()
        {
            var builder = new WebApplicationBuilder(new(), bootstrapBuilder =>
            {
                bootstrapBuilder.ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        [WebHostDefaults.ServerUrlsKey] = "http://localhost:5000",
                    });
                });
            });

            builder.WebHost.UseUrls("http://localhost:5001");

            var urls = new List<string>();
            var server = new MockAddressesServer(urls);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            await app.StartAsync();

            var url = Assert.Single(urls);
            Assert.Equal("http://localhost:5001", url);
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
        public async Task HostedServicesRunBeforeTheServerStarts()
        {
            var builder = WebApplication.CreateBuilder();
            var startOrder = new List<object>();
            var server = new MockServer(startOrder);
            var hostedService = new HostedService(startOrder);
            builder.Services.AddSingleton<IHostedService>(hostedService);
            builder.Services.AddSingleton<IServer>(server);
            await using var app = builder.Build();

            await app.StartAsync();

            Assert.Equal(2, startOrder.Count);
            Assert.Same(hostedService, startOrder[0]);
            Assert.Same(server, startOrder[1]);
        }

        class HostedService : IHostedService
        {
            private readonly List<object> _startOrder;

            public HostedService(List<object> startOrder)
            {
                _startOrder = startOrder;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                _startOrder.Add(this);
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        class MockServer : IServer
        {
            private readonly List<object> _startOrder;

            public MockServer(List<object> startOrder)
            {
                _startOrder = startOrder;
            }

            public IFeatureCollection Features { get; } = new FeatureCollection();

            public void Dispose() { }

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
            {
                _startOrder.Add(this);
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
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

            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var webRoot = Path.Combine(contentRoot, "wwwroot");
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ApplicationKey, nameof(WebApplicationTests)));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.EnvironmentKey, envName));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.ContentRootKey, contentRoot));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.WebRootKey, webRoot));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "hosting"));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, "hostingexclude"));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseEnvironment(envName));
            Assert.Throws<NotSupportedException>(() => builder.WebHost.UseContentRoot(contentRoot));
        }

        [Fact]
        public void WebApplicationBuilderWebHostSettingsThatAffectTheHostCannotBeModifiedViaConfigureAppConfiguration()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var webRoot = Path.Combine(contentRoot, "wwwroot");
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
                    { WebHostDefaults.WebRootKey, webRoot }
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
        public void SettingContentRootToSameCanonicalValueWorks()
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(contentRoot);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRoot
            });

            builder.Host.UseContentRoot(contentRoot + Path.DirectorySeparatorChar);
            builder.Host.UseContentRoot(contentRoot.ToUpperInvariant());
            builder.Host.UseContentRoot(contentRoot.ToLowerInvariant());

            builder.WebHost.UseContentRoot(contentRoot + Path.DirectorySeparatorChar);
            builder.WebHost.UseContentRoot(contentRoot.ToUpperInvariant());
            builder.WebHost.UseContentRoot(contentRoot.ToLowerInvariant());
        }

        [Theory]
        [InlineData("wwwroot2")]
        [InlineData("./wwwroot2")]
        [InlineData("./bar/../wwwroot2")]
        [InlineData("foo/../wwwroot2")]
        [InlineData("wwwroot2/.")]
        public void WebApplicationBuilder_CanHandleVariousWebRootPaths(string webRoot)
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var fullWebRootPath = Path.Combine(contentRoot, "wwwroot2");

            try
            {
                var options = new WebApplicationOptions
                {
                    ContentRootPath = contentRoot,
                    WebRootPath = "wwwroot2"
                };

                var builder = new WebApplicationBuilder(options);

                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

                builder.WebHost.UseWebRoot(webRoot);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Fact]
        public void WebApplicationBuilder_CanOverrideWithFullWebRootPaths()
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var fullWebRootPath = Path.Combine(contentRoot, "wwwroot");
            Directory.CreateDirectory(fullWebRootPath);

            try
            {
                var options = new WebApplicationOptions
                {
                    ContentRootPath = contentRoot,
                };

                var builder = new WebApplicationBuilder(options);

                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

                builder.WebHost.UseWebRoot(fullWebRootPath);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Theory]
        [InlineData("wwwroot")]
        [InlineData("./wwwroot")]
        [InlineData("./bar/../wwwroot")]
        [InlineData("foo/../wwwroot")]
        [InlineData("wwwroot/.")]
        public void WebApplicationBuilder_CanHandleVariousWebRootPaths_OverrideDefaultPath(string webRoot)
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var fullWebRootPath = Path.Combine(contentRoot, "wwwroot");
            Directory.CreateDirectory(fullWebRootPath);

            try
            {
                var options = new WebApplicationOptions
                {
                    ContentRootPath = contentRoot
                };

                var builder = new WebApplicationBuilder(options);

                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);

                builder.WebHost.UseWebRoot(webRoot);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Theory]
        [InlineData("")]  // Empty behaves differently to null
        [InlineData(".")]
        public void SettingContentRootToRelativePathUsesAppContextBaseDirectoryAsPathBase(string path)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = path
            });

            builder.Host.UseContentRoot(AppContext.BaseDirectory);
            builder.Host.UseContentRoot(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            builder.Host.UseContentRoot("");

            builder.WebHost.UseContentRoot(AppContext.BaseDirectory);
            builder.WebHost.UseContentRoot(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            builder.WebHost.UseContentRoot("");

            Assert.Equal(AppContext.BaseDirectory, builder.Environment.ContentRootPath);
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
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var webRoot = "wwwroot2";
            var fullWebRootPath = Path.Combine(contentRoot, webRoot);
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            try
            {
                var options = new WebApplicationOptions
                {
                    ApplicationName = nameof(WebApplicationTests),
                    ContentRootPath = contentRoot,
                    EnvironmentName = envName,
                    WebRootPath = webRoot
                };

                var builder = new WebApplicationBuilder(
                    options,
                    bootstrapBuilder =>
                    {
                        bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                        {
                            Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                            Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                            Assert.Equal(contentRoot + Path.DirectorySeparatorChar, context.HostingEnvironment.ContentRootPath);
                        });
                    });

                Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
                Assert.Equal(envName, builder.Environment.EnvironmentName);
                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Fact]
        public void WebApplicationBuilderWebApplicationOptionsPropertiesOverridesArgs()
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var webRoot = "wwwroot2";
            var fullWebRootPath = Path.Combine(contentRoot, webRoot);
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            try
            {
                var options = new WebApplicationOptions
                {
                    Args = new[] {
                        $"--{WebHostDefaults.ApplicationKey}=testhost",
                        $"--{WebHostDefaults.ContentRootKey}={contentRoot}",
                        $"--{WebHostDefaults.WebRootKey}=wwwroot2",
                        $"--{WebHostDefaults.EnvironmentKey}=Test"
                    },
                    ApplicationName = nameof(WebApplicationTests),
                    ContentRootPath = contentRoot,
                    EnvironmentName = envName,
                    WebRootPath = webRoot
                };

                var builder = new WebApplicationBuilder(
                    options,
                    bootstrapBuilder =>
                    {
                        bootstrapBuilder.ConfigureAppConfiguration((context, config) =>
                        {
                            Assert.Equal(nameof(WebApplicationTests), context.HostingEnvironment.ApplicationName);
                            Assert.Equal(envName, context.HostingEnvironment.EnvironmentName);
                            Assert.Equal(contentRoot + Path.DirectorySeparatorChar, context.HostingEnvironment.ContentRootPath);
                        });
                    });

                Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
                Assert.Equal(envName, builder.Environment.EnvironmentName);
                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Fact]
        public void WebApplicationBuilderCanConfigureHostSettingsUsingWebApplicationOptionsArgs()
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(contentRoot);
            var webRoot = "wwwroot";
            var fullWebRootPath = Path.Combine(contentRoot, webRoot);
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            try
            {

                var options = new WebApplicationOptions
                {
                    Args = new[] {
                        $"--{WebHostDefaults.ApplicationKey}={nameof(WebApplicationTests)}",
                        $"--{WebHostDefaults.ContentRootKey}={contentRoot}",
                        $"--{WebHostDefaults.EnvironmentKey}={envName}",
                        $"--{WebHostDefaults.WebRootKey}={webRoot}",
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
                            Assert.Equal(contentRoot + Path.DirectorySeparatorChar, context.HostingEnvironment.ContentRootPath);
                        });
                    });

                Assert.Equal(nameof(WebApplicationTests), builder.Environment.ApplicationName);
                Assert.Equal(envName, builder.Environment.EnvironmentName);
                Assert.Equal(contentRoot + Path.DirectorySeparatorChar, builder.Environment.ContentRootPath);
                Assert.Equal(fullWebRootPath, builder.Environment.WebRootPath);
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
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
        public async Task WebApplicationCanObserveSourcesClearedInBuild()
        {
            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureHostConfiguration(config =>
                {
                    // Clearing here would not remove the app config added via builder.Configuration.
                    config.AddInMemoryCollection(new Dictionary<string, string>()
                    {
                        { "A", "A" },
                    });
                });

                hostBuilder.ConfigureAppConfiguration(config =>
                {
                    // This clears both the chained host configuration and chained builder.Configuration.
                    config.Sources.Clear();
                    config.AddInMemoryCollection(new Dictionary<string, string>()
                    {
                        { "B", "B" },
                    });
                });
            });

            var builder = WebApplication.CreateBuilder();

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "C", "C" },
            });

            await using var app = builder.Build();

            Assert.True(string.IsNullOrEmpty(app.Configuration["A"]));
            Assert.True(string.IsNullOrEmpty(app.Configuration["C"]));

            Assert.Equal("B", app.Configuration["B"]);

            Assert.Same(builder.Configuration, app.Configuration);
        }

        [Fact]
        public async Task WebApplicationCanHandleStreamBackedConfigurationAddedInBuild()
        {
            static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

            using var jsonAStream = CreateStreamFromString(@"{ ""A"": ""A"" }");
            using var jsonBStream = CreateStreamFromString(@"{ ""B"": ""B"" }");

            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureHostConfiguration(config => config.AddJsonStream(jsonAStream));
                hostBuilder.ConfigureAppConfiguration(config => config.AddJsonStream(jsonBStream));
            });

            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            Assert.Equal("A", app.Configuration["A"]);
            Assert.Equal("B", app.Configuration["B"]);

            Assert.Same(builder.Configuration, app.Configuration);
        }

        [Fact]
        public async Task WebApplicationDisposesConfigurationProvidersAddedInBuild()
        {
            var hostConfigSource = new RandomConfigurationSource();
            var appConfigSource = new RandomConfigurationSource();

            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureHostConfiguration(config => config.Add(hostConfigSource));
                hostBuilder.ConfigureAppConfiguration(config => config.Add(appConfigSource));
            });

            var builder = WebApplication.CreateBuilder();

            {
                await using var app = builder.Build();

                Assert.Equal(1, hostConfigSource.ProvidersBuilt);
                Assert.Equal(1, appConfigSource.ProvidersBuilt);
                Assert.Equal(1, hostConfigSource.ProvidersLoaded);
                Assert.Equal(1, appConfigSource.ProvidersLoaded);
                Assert.Equal(0, hostConfigSource.ProvidersDisposed);
                Assert.Equal(0, appConfigSource.ProvidersDisposed);
            }

            Assert.Equal(1, hostConfigSource.ProvidersBuilt);
            Assert.Equal(1, appConfigSource.ProvidersBuilt);
            Assert.Equal(1, hostConfigSource.ProvidersLoaded);
            Assert.Equal(1, appConfigSource.ProvidersLoaded);
            Assert.True(hostConfigSource.ProvidersDisposed > 0);
            Assert.True(appConfigSource.ProvidersDisposed > 0);
        }

        [Fact]
        public async Task WebApplicationMakesOriginalConfigurationProvidersAddedInBuildAccessable()
        {
            // This mimics what WebApplicationFactory<T> does and runs configure
            // services callbacks
            using var listener = new HostingListener(hostBuilder =>
            {
                hostBuilder.ConfigureAppConfiguration(config => config.Add(new RandomConfigurationSource()));
            });

            var builder = WebApplication.CreateBuilder();
            await using var app = builder.Build();

            var wrappedProviders = ((IConfigurationRoot)app.Configuration).Providers.OfType<IEnumerable<IConfigurationProvider>>();
            var unwrappedProviders = wrappedProviders.Select(p => Assert.Single(p));
            Assert.Single(unwrappedProviders.OfType<RandomConfigurationProvider>());
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
        public void CanResolveIConfigurationBeforeBuildingApplication()
        {
            var builder = WebApplication.CreateBuilder();
            var sp = builder.Services.BuildServiceProvider();

            var config = sp.GetService<IConfiguration>();
            Assert.NotNull(config);
            Assert.Same(config, builder.Configuration);

            var app = builder.Build();

            Assert.Same(app.Configuration, builder.Configuration);
        }

        [Fact]
        public void ManuallyAddingConfigurationAsServiceWorks()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            var sp = builder.Services.BuildServiceProvider();

            var config = sp.GetService<IConfiguration>();
            Assert.NotNull(config);
            Assert.Same(config, builder.Configuration);

            var app = builder.Build();

            Assert.Same(app.Configuration, builder.Configuration);
        }

        [Fact]
        public void AddingMemoryStreamBackedConfigurationWorks()
        {
            var builder = WebApplication.CreateBuilder();

            var jsonConfig = @"{ ""foo"": ""bar"" }";
            using var ms = new MemoryStream();
            using var sw = new StreamWriter(ms);
            sw.WriteLine(jsonConfig);
            sw.Flush();

            ms.Position = 0;
            builder.Configuration.AddJsonStream(ms);

            Assert.Equal("bar", builder.Configuration["foo"]);

            var app = builder.Build();

            Assert.Equal("bar", app.Configuration["foo"]);
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

        [Fact]
        public async Task WebApplication_CanCallUseEndpointsWithoutUseRoutingFails()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/1", () => "1");

            var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoints(endpoints => { }));
            Assert.Contains("UseRouting", ex.Message);
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
            builder.Services.AddSingleton<IStartupFilter, UseRoutingStartupFilter>();
            await using var app = builder.Build();

            var chosenEndpoint = string.Empty;
            app.MapGet("/", async c =>
            {
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

        [Fact]
        public async Task CanAddMiddlewareBeforeUseRouting()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            var chosenEndpoint = string.Empty;

            app.Use((c, n) =>
            {
                chosenEndpoint = c.GetEndpoint()?.DisplayName;
                Assert.Null(c.GetEndpoint());
                return n(c);
            });

            app.UseRouting();

            app.MapGet("/1", async c =>
            {
                chosenEndpoint = c.GetEndpoint().DisplayName;
                await c.Response.WriteAsync("Hello World");
            }).WithDisplayName("One");

            app.UseEndpoints(e => { });

            await app.StartAsync();

            var client = app.GetTestClient();

            _ = await client.GetAsync("http://localhost/");
            Assert.Null(chosenEndpoint);

            _ = await client.GetAsync("http://localhost/1");
            Assert.Equal("One", chosenEndpoint);
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

            var ex = Assert.Throws<NotSupportedException>(() => builder.WebHost.Configure(app => { }));
            var ex1 = Assert.Throws<NotSupportedException>(() => builder.WebHost.Configure((context, app) => { }));
            var ex2 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup<MyStartup>());
            var ex3 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup(typeof(MyStartup)));
            var ex4 = Assert.Throws<NotSupportedException>(() => builder.WebHost.UseStartup(context => new MyStartup()));

            Assert.Equal("Configure() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex.Message);
            Assert.Equal("Configure() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex1.Message);
            Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex2.Message);
            Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex3.Message);
            Assert.Equal("UseStartup() is not supported by WebApplicationBuilder.WebHost. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex4.Message);

            var ex5 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }));
            var ex6 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHost(webHostBuilder => { }, options => { }));
            var ex7 = Assert.Throws<NotSupportedException>(() => builder.Host.ConfigureWebHostDefaults(webHostBuilder => { }));

            Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex5.Message);
            Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex6.Message);
            Assert.Equal("ConfigureWebHost() is not supported by WebApplicationBuilder.Host. Use the WebApplication returned by WebApplicationBuilder.Build() instead.", ex7.Message);
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
        public async Task WebApplication_CallsUseRoutingAndUseEndpoints()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            var chosenRoute = string.Empty;
            app.MapGet("/", async c =>
            {
                chosenRoute = c.GetEndpoint()?.DisplayName;
                await c.Response.WriteAsync("Hello World");
            }).WithDisplayName("One");

            await app.StartAsync();

            var ds = app.Services.GetRequiredService<EndpointDataSource>();
            Assert.Equal(1, ds.Endpoints.Count);
            Assert.Equal("One", ds.Endpoints[0].DisplayName);

            var client = app.GetTestClient();

            _ = await client.GetAsync("http://localhost/");
            Assert.Equal("One", chosenRoute);
        }

        [Fact]
        public async Task BranchingPipelineHasOwnRoutes()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.UseRouting();

            var chosenRoute = string.Empty;
            app.MapGet("/", () => "Hello World!").WithDisplayName("One");

            app.UseEndpoints(routes =>
            {
                routes.MapGet("/hi", async c =>
                {
                    chosenRoute = c.GetEndpoint()?.DisplayName;
                    await c.Response.WriteAsync("Hello World");
                }).WithDisplayName("Two");
                routes.MapGet("/heyo", () => "Heyo World").WithDisplayName("Three");
            });

            var newBuilder = ((IApplicationBuilder)app).New();
            Assert.False(newBuilder.Properties.TryGetValue(WebApplication.GlobalEndpointRouteBuilderKey, out _));

            newBuilder.UseRouting();
            newBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/h3", async c =>
                {
                    chosenRoute = c.GetEndpoint()?.DisplayName;
                    await c.Response.WriteAsync("Hello World");
                }).WithDisplayName("Four");
                endpoints.MapGet("hi", async c =>
                {
                    chosenRoute = c.GetEndpoint()?.DisplayName;
                    await c.Response.WriteAsync("Hi New");
                }).WithDisplayName("Five");
            });
            var branch = newBuilder.Build();
            app.Run(c => branch(c));

            app.Start();

            var ds = app.Services.GetRequiredService<EndpointDataSource>();
            Assert.Equal(5, ds.Endpoints.Count);
            Assert.Equal("One", ds.Endpoints[0].DisplayName);
            Assert.Equal("Two", ds.Endpoints[1].DisplayName);
            Assert.Equal("Three", ds.Endpoints[2].DisplayName);
            Assert.Equal("Four", ds.Endpoints[3].DisplayName);
            Assert.Equal("Five", ds.Endpoints[4].DisplayName);

            var client = app.GetTestClient();

            // '/hi' routes don't conflict and the non-branched one is chosen
            _ = await client.GetAsync("http://localhost/hi");
            Assert.Equal("Two", chosenRoute);

            // Can access branched routes
            _ = await client.GetAsync("http://localhost/h3");
            Assert.Equal("Four", chosenRoute);
        }

        [Fact]
        public async Task PropertiesPreservedFromInnerApplication()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IStartupFilter, PropertyFilter>();
            await using var app = builder.Build();

            ((IApplicationBuilder)app).Properties["didsomething"] = true;

            app.Start();
        }

        [Fact]
        public async Task DeveloperExceptionPageIsOnByDefaltInDevelopment()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/", void () => throw new InvalidOperationException("BOOM"));

            await app.StartAsync();

            var client = app.GetTestClient();

            var response = await client.GetAsync("/");

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("BOOM", await response.Content.ReadAsStringAsync());
            Assert.Contains("text/plain", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task DeveloperExceptionPageDoesNotGetCaughtByStartupFilters()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<IStartupFilter, ThrowingStartupFilter>();
            await using var app = builder.Build();

            await app.StartAsync();

            var client = app.GetTestClient();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/"));

            Assert.Equal("BOOM Filter", ex.Message);
        }

        [Fact]
        public async Task DeveloperExceptionPageIsNotOnInProduction()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Production });
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/", void () => throw new InvalidOperationException("BOOM"));

            await app.StartAsync();

            var client = app.GetTestClient();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/"));

            Assert.Equal("BOOM", ex.Message);
        }

        [Fact]
        public async Task HostingStartupRunsWhenApplicationIsNotEntryPoint()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(WebApplicationTests).Assembly.FullName });
            await using var app = builder.Build();

            Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
        }

        [Fact]
        public async Task HostingStartupRunsWhenApplicationIsNotEntryPointWithArgs()
        {
            var builder = WebApplication.CreateBuilder(new[] { "--applicationName", typeof(WebApplicationTests).Assembly.FullName });
            await using var app = builder.Build();

            Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
        }

        [Fact]
        public async Task HostingStartupRunsWhenApplicationIsNotEntryPointApplicationNameWinsOverArgs()
        {
            var options = new WebApplicationOptions
            {
                Args = new[] { "--applicationName", typeof(WebApplication).Assembly.FullName },
                ApplicationName = typeof(WebApplicationTests).Assembly.FullName,
            };
            var builder = WebApplication.CreateBuilder(options);
            await using var app = builder.Build();

            Assert.Equal("value", app.Configuration["testhostingstartup:config"]);
        }

        [Fact]
        public async Task DeveloperExceptionPageWritesBadRequestDetailsToResponseByDefaltInDevelopment()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/{parameterName}", (int parameterName) => { });

            await app.StartAsync();

            var client = app.GetTestClient();

            var response = await client.GetAsync("/notAnInt");

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("text/plain", response.Content.Headers.ContentType.MediaType);

            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("parameterName", responseBody);
            Assert.Contains("notAnInt", responseBody);
        }

        [Fact]
        public async Task NoExceptionAreThrownForBadRequestsInProduction()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Production });
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();

            app.MapGet("/{parameterName}", (int parameterName) => { });

            await app.StartAsync();

            var client = app.GetTestClient();

            var response = await client.GetAsync("/notAnInt");

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Null(response.Content.Headers.ContentType);

            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, responseBody);
        }

        [Fact]
        public void EmptyAppConfiguration()
        {
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            bool createdDirectory = false;
            if (!Directory.Exists(wwwroot))
            {
                createdDirectory = true;
                Directory.CreateDirectory(wwwroot);
            }

            try
            {
                var builder = WebApplication.CreateBuilder();

                builder.WebHost.ConfigureAppConfiguration((ctx, config) => { });

                using var app = builder.Build();
                var hostEnv = app.Services.GetRequiredService<Hosting.IWebHostEnvironment>();
                Assert.Equal(wwwroot, hostEnv.WebRootPath);
            }
            finally
            {
                if (createdDirectory)
                {
                    Directory.Delete(wwwroot);
                }
            }
        }

        [Fact]
        public void HostConfigurationNotAffectedByConfiguration()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            builder.Configuration[WebHostDefaults.ApplicationKey] = nameof(WebApplicationTests);
            builder.Configuration[WebHostDefaults.EnvironmentKey] = envName;
            builder.Configuration[WebHostDefaults.ContentRootKey] = contentRoot;

            var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
            var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal(builder.Environment.ApplicationName, hostEnv.ApplicationName);
            Assert.Equal(builder.Environment.EnvironmentName, hostEnv.EnvironmentName);
            Assert.Equal(builder.Environment.ContentRootPath, hostEnv.ContentRootPath);

            Assert.Equal(webHostEnv.ApplicationName, hostEnv.ApplicationName);
            Assert.Equal(webHostEnv.EnvironmentName, hostEnv.EnvironmentName);
            Assert.Equal(webHostEnv.ContentRootPath, hostEnv.ContentRootPath);

            Assert.NotEqual(nameof(WebApplicationTests), hostEnv.ApplicationName);
            Assert.NotEqual(envName, hostEnv.EnvironmentName);
            Assert.NotEqual(contentRoot, hostEnv.ContentRootPath);
        }

        [Fact]
        public void ClearingConfigurationDoesNotAffectHostConfiguration()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(WebApplicationOptions).Assembly.FullName,
                EnvironmentName = Environments.Staging,
                ContentRootPath = Path.GetTempPath()
            });

            ((IConfigurationBuilder)builder.Configuration).Sources.Clear();

            var app = builder.Build();
            var hostEnv = app.Services.GetRequiredService<IHostEnvironment>();
            var webHostEnv = app.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal(builder.Environment.ApplicationName, hostEnv.ApplicationName);
            Assert.Equal(builder.Environment.EnvironmentName, hostEnv.EnvironmentName);
            Assert.Equal(builder.Environment.ContentRootPath, hostEnv.ContentRootPath);

            Assert.Equal(webHostEnv.ApplicationName, hostEnv.ApplicationName);
            Assert.Equal(webHostEnv.EnvironmentName, hostEnv.EnvironmentName);
            Assert.Equal(webHostEnv.ContentRootPath, hostEnv.ContentRootPath);

            Assert.Equal(typeof(WebApplicationOptions).Assembly.FullName, hostEnv.ApplicationName);
            Assert.Equal(Environments.Staging, hostEnv.EnvironmentName);
            Assert.Equal(Path.GetTempPath(), hostEnv.ContentRootPath);
        }

        [Fact]
        public void ConfigurationGetDebugViewWorks()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["foo"] = "bar",
            });

            var app = builder.Build();

            // Make sure we don't lose "MemoryConfigurationProvider" from GetDebugView() when wrapping the provider.
            Assert.Contains("foo=bar (MemoryConfigurationProvider)", ((IConfigurationRoot)app.Configuration).GetDebugView());
        }

        [Fact]
        public void ConfigurationCanBeReloaded()
        {
            var builder = WebApplication.CreateBuilder();

            ((IConfigurationBuilder)builder.Configuration).Sources.Add(new RandomConfigurationSource());

            var app = builder.Build();

            var value0 = app.Configuration["Random"];
            ((IConfigurationRoot)app.Configuration).Reload();
            var value1 = app.Configuration["Random"];

            Assert.NotEqual(value0, value1);
        }

        [Fact]
        public void ConfigurationSourcesAreBuiltOnce()
        {
            var builder = WebApplication.CreateBuilder();

            var configSource = new RandomConfigurationSource();
            ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

            var app = builder.Build();

            Assert.Equal(1, configSource.ProvidersBuilt);
        }

        [Fact]
        public void ConfigurationProvidersAreLoadedOnceAfterBuild()
        {
            var builder = WebApplication.CreateBuilder();

            var configSource = new RandomConfigurationSource();
            ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

            using var app = builder.Build();

            Assert.Equal(1, configSource.ProvidersLoaded);
        }

        [Fact]
        public void ConfigurationProvidersAreDisposedWithWebApplication()
        {
            var builder = WebApplication.CreateBuilder();

            var configSource = new RandomConfigurationSource();
            ((IConfigurationBuilder)builder.Configuration).Sources.Add(configSource);

            {
                using var app = builder.Build();

                Assert.Equal(0, configSource.ProvidersDisposed);
            }

            Assert.Equal(1, configSource.ProvidersDisposed);
        }

        [Fact]
        public void ConfigurationProviderTypesArePreserved()
        {
            var builder = WebApplication.CreateBuilder();

            ((IConfigurationBuilder)builder.Configuration).Sources.Add(new RandomConfigurationSource());

            var app = builder.Build();

            Assert.Single(((IConfigurationRoot)app.Configuration).Providers.OfType<RandomConfigurationProvider>());
        }

        public class RandomConfigurationSource : IConfigurationSource
        {
            public int ProvidersBuilt { get; set; }
            public int ProvidersLoaded { get; set; }
            public int ProvidersDisposed { get; set; }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                ProvidersBuilt++;
                return new RandomConfigurationProvider(this);
            }
        }

        public class RandomConfigurationProvider : ConfigurationProvider, IDisposable
        {
            private readonly RandomConfigurationSource _source;

            public RandomConfigurationProvider(RandomConfigurationSource source)
            {
                _source = source;
            }

            public override void Load()
            {
                _source.ProvidersLoaded++;
                Data["Random"] = Guid.NewGuid().ToString();
            }

            public void Dispose() => _source.ProvidersDisposed++;
        }

        public class TestHostingStartup : IHostingStartup
        {
            public void Configure(IWebHostBuilder builder)
            {
                builder
                    .ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.AddInMemoryCollection(
                        new[]
                        {
                            new KeyValuePair<string,string>("testhostingstartup:config", "value")
                        }));
            }
        }

        class ThrowingStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.Use((HttpContext context, RequestDelegate next) =>
                    {
                        throw new InvalidOperationException("BOOM Filter");
                    });

                    next(app);
                };
            }
        }

        class PropertyFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);

                    // This should be true
                    var val = app.Properties["didsomething"];
                    Assert.True((bool)val);
                };
            }
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

        class UseRoutingStartupFilter : IStartupFilter
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
    }
}
