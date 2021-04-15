// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
        public void WebApplicationBuilderConfiguration_IncludesCommandLineArguments()
        {
            var builder = WebApplication.CreateBuilder(new string[] { "--urls", "http://localhost:5001" });
            Assert.Equal("http://localhost:5001", builder.Configuration["urls"]);
        }

        [Fact]
        public void WebApplicationListen_UpdatesIServerAddressesFeature()
        {
            var app = WebApplication.Create();
            app.Listen("http://localhost:5001");

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
            var address = Assert.Single(addresses);
            Assert.Equal("http://localhost:5001", address);
        }

        [Fact]
        public void WebApplicationBuilderServer_ThrowsWhenBuilt()
        {
            Assert.Throws<NotSupportedException>(() => WebApplication.CreateBuilder().Server.Build());
        }

        [Fact]
        public async Task WebApplicationConfiguration_HostFilterOptionsAreReloadable()
        {
            var builder = WebApplication.CreateBuilder();
            var host = builder.Server
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
            builder.Server.UseTestServer();
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
    }
}
