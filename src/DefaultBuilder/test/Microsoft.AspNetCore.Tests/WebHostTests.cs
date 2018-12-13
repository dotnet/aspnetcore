// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class WebHostTests
    {
        [Fact]
        public void WebHostConfiguration_IncludesCommandLineArguments()
        {
            var builder = WebHost.CreateDefaultBuilder(new string[] { "--urls", "http://localhost:5001" });
            Assert.Equal("http://localhost:5001", builder.GetSetting(WebHostDefaults.ServerUrlsKey));
        }

        [Fact]
        public void WebHostConfiguration_HostFilterOptionsAreReloadable()
        {
            var host = WebHost.CreateDefaultBuilder()
                .Configure(app => { })
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.Add(new ReloadableMemorySource());
                }).Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            var monitor = host.Services.GetRequiredService<IOptionsMonitor<HostFilteringOptions>>();
            var options = monitor.CurrentValue;

            Assert.Contains("*", options.AllowedHosts);

            var changed = new ManualResetEvent(false);
            monitor.OnChange(newOptions =>
            {
                changed.Set();
            });

            config["AllowedHosts"] = "NewHost";

            Assert.True(changed.WaitOne(TimeSpan.FromSeconds(10)));
            options = monitor.CurrentValue;
            Assert.Contains("NewHost", options.AllowedHosts);
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
