// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class WebHostEnvironmentTests
    {
        [Fact]
        public void ApplyConfigurationSettingsUsesTheCorrectKeys()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>(WebHostDefaults.ApplicationKey, WebHostDefaults.ApplicationKey),
                new KeyValuePair<string, string>(WebHostDefaults.EnvironmentKey, WebHostDefaults.EnvironmentKey),
                new KeyValuePair<string, string>(WebHostDefaults.ContentRootKey, WebHostDefaults.ContentRootKey),
                new KeyValuePair<string, string>(WebHostDefaults.WebRootKey, WebHostDefaults.WebRootKey),
            });

            var env = new WebHostEnvironment();

            // Basically call ApplyConfigurationSettings(config) but without creating PhysicalFileProviders.
            env.ReadConfigurationSettings(configBuilder.Build());

            Assert.Equal(WebHostDefaults.ApplicationKey, env.ApplicationName);
            Assert.Equal(WebHostDefaults.EnvironmentKey, env.EnvironmentName);
            Assert.Equal(WebHostDefaults.ContentRootKey, env.ContentRootPath);
            Assert.Equal(WebHostDefaults.WebRootKey, env.WebRootPath);
        }

        [Fact]
        public void ApplyEnvironmentSettingsUsesTheCorrectKeys()
        {
            var env = new WebHostEnvironment
            {
                ApplicationName = WebHostDefaults.ApplicationKey,
                EnvironmentName = WebHostDefaults.EnvironmentKey,
                ContentRootPath = WebHostDefaults.ContentRootKey,
                WebRootPath = WebHostDefaults.WebRootKey,
            };

            var settings = new Dictionary<string, string>();

            env.ApplyEnvironmentSettings(new TestWebHostBuilder(settings));

            Assert.Equal(WebHostDefaults.ApplicationKey, settings[WebHostDefaults.ApplicationKey]);
            Assert.Equal(WebHostDefaults.EnvironmentKey, settings[WebHostDefaults.EnvironmentKey]);
            Assert.Equal(WebHostDefaults.ContentRootKey, settings[WebHostDefaults.ContentRootKey]);
            Assert.Equal(WebHostDefaults.WebRootKey, settings[WebHostDefaults.WebRootKey]);
        }

        private class TestWebHostBuilder : IWebHostBuilder
        {
            private readonly Dictionary<string, string> _settings;

            public TestWebHostBuilder(Dictionary<string, string> settingsDictionary)
            {
                _settings = settingsDictionary;
            }

            public IWebHost Build()
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
            {
                throw new NotImplementedException();
            }

            public string GetSetting(string key)
            {
                _settings.TryGetValue(key, out var value);
                return value;
            }

            public IWebHostBuilder UseSetting(string key, string value)
            {
                _settings[key] = value;
                return this;
            }
        }
    }
}
