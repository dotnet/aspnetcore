// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public async Task WebApplicationAddresses_UpdatesIServerAddressesFeature()
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
        public void WebApplicationBuilderWebHostUseSettings_IsCaseInsensitive()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Path.GetTempPath().ToString();
            var webRoot = Path.GetTempPath().ToString();
            var envName = $"{nameof(WebApplicationTests)}_ENV";

            builder.WebHost.UseSetting("applicationname", nameof(WebApplicationTests));

            builder.WebHost.UseSetting("ENVIRONMENT", envName);
            builder.WebHost.UseSetting("CONTENTROOT", contentRoot);
            builder.WebHost.UseSetting("WEBROOT", webRoot);

            Assert.Equal(nameof(WebApplicationTests), builder.WebHost.GetSetting("APPLICATIONNAME"));
            Assert.Equal(envName, builder.WebHost.GetSetting("environment"));
            Assert.Equal(contentRoot, builder.WebHost.GetSetting("contentroot"));
            Assert.Equal(webRoot, builder.WebHost.GetSetting("webroot"));

            var app = builder.Build();

            Assert.Equal(nameof(WebApplicationTests), app.Environment.ApplicationName);
            Assert.Equal(envName, app.Environment.EnvironmentName);
            Assert.Equal(contentRoot, app.Environment.ContentRootPath);
            Assert.Equal(webRoot, app.Environment.WebRootPath);
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
                changed.SetResult(0);
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
            builder.Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("FORWARDEDHEADERS_ENABLED", "true" ),
            });
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
    }
}
