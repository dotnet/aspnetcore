// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Moq;
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
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), WebHostDefaults.ContentRootKey), env.ContentRootPath);
            var fullWebRootPath = Path.Combine(env.ContentRootPath, env.WebRootPath);
            Assert.Equal(fullWebRootPath, env.WebRootPath);
        }

        [Fact]
        public void ApplyEnvironmentSettingsUsesTheCorrectKeysAndProperties()
        {
            var originalEnvironment = new WebHostEnvironment
            {
                ApplicationName = WebHostDefaults.ApplicationKey,
                EnvironmentName = WebHostDefaults.EnvironmentKey,
                ContentRootPath = WebHostDefaults.ContentRootKey,
                WebRootPath = WebHostDefaults.WebRootKey,
                ContentRootFileProvider = Mock.Of<IFileProvider>(),
                WebRootFileProvider = Mock.Of<IFileProvider>(),
            };

            var settings = new Dictionary<string, string>();
            var webHostBuilderEnvironment = new WebHostEnvironment();

            originalEnvironment.ApplyEnvironmentSettings(new TestWebHostBuilder(settings, webHostBuilderEnvironment));

            Assert.Equal(WebHostDefaults.ApplicationKey, settings[WebHostDefaults.ApplicationKey]);
            Assert.Equal(WebHostDefaults.EnvironmentKey, settings[WebHostDefaults.EnvironmentKey]);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), WebHostDefaults.ContentRootKey), settings[WebHostDefaults.ContentRootKey]);
            var fullWebRootPath = Path.Combine(settings[WebHostDefaults.ContentRootKey], settings[WebHostDefaults.WebRootKey]);
            Assert.Equal(fullWebRootPath, settings[WebHostDefaults.WebRootKey]);

            Assert.Equal(WebHostDefaults.ApplicationKey, webHostBuilderEnvironment.ApplicationName);
            Assert.Equal(WebHostDefaults.EnvironmentKey, webHostBuilderEnvironment.EnvironmentName);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), WebHostDefaults.ContentRootKey), webHostBuilderEnvironment.ContentRootPath);
            Assert.Equal(fullWebRootPath, webHostBuilderEnvironment.WebRootPath);

            Assert.NotEqual(originalEnvironment.ContentRootFileProvider, webHostBuilderEnvironment.ContentRootFileProvider);
            Assert.NotEqual(originalEnvironment.WebRootFileProvider, webHostBuilderEnvironment.WebRootFileProvider);
        }

        [Fact]
        public void SettingPathsSetsContentProviders()
        {
            var environment = new WebHostEnvironment();
            var tempPath = Path.GetTempPath();

            environment.ContentRootPath = tempPath;
            environment.WebRootPath = tempPath;

            Assert.Equal(tempPath, environment.WebRootPath);
            Assert.Equal(tempPath, environment.ContentRootPath);

            Assert.IsType<PhysicalFileProvider>(environment.ContentRootFileProvider);
            Assert.IsType<PhysicalFileProvider>(environment.WebRootFileProvider);

            Assert.Equal(tempPath, ((PhysicalFileProvider)environment.ContentRootFileProvider).Root);
            Assert.Equal(tempPath, ((PhysicalFileProvider)environment.WebRootFileProvider).Root);
        }

        [Fact]
        public void RelativePathsAreMappedToFullPaths()
        {
            var environment = new WebHostEnvironment();
            var relativeRootPath = "some-relative-path";
            var relativeSubPath = "some-other-relative-path";
            var fullContentRoot = Path.Combine(Directory.GetCurrentDirectory(), relativeRootPath);
            
            // ContentRootPath is mapped relative to Directory.GetCurrentDirectory()
            environment.ContentRootPath = relativeRootPath;
            Assert.Equal(fullContentRoot, environment.ContentRootPath);

            // WebRootPath is mapped relative to ContentRootPath
            environment.WebRootPath = relativeSubPath;
            Assert.Equal(Path.Combine(fullContentRoot, relativeSubPath), environment.WebRootPath);
        }

        [Fact]
        public void UnsettingPathsFallsBackToDefaults()
        {
            var environment = new WebHostEnvironment();
            var defaultWebRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            var webRootPath = Path.GetTempPath();

            environment.WebRootPath = webRootPath;

            Assert.Equal(webRootPath, environment.WebRootPath);
            Assert.Equal(webRootPath, ((PhysicalFileProvider)environment.WebRootFileProvider).Root);

            // Setting WebRootPath to fallsback to default
            environment.WebRootPath = null;
            Assert.Equal(defaultWebRootPath, environment.WebRootPath);

            // Setting ContentRootPath to null falls back to current directory
            environment.ContentRootPath = null;
            Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
            Assert.Equal(AppContext.BaseDirectory, ((PhysicalFileProvider)environment.ContentRootFileProvider).Root);
        }

        [Fact]
        public void SetContentRootAfterRelativeWebRoot()
        {
            var environment = new WebHostEnvironment();
            var webRootPath = "some-relative-path";
            var tempPath = Path.GetTempPath();

            environment.WebRootPath = webRootPath;

            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), webRootPath), environment.WebRootPath);
            Assert.Equal(Directory.GetCurrentDirectory(), environment.ContentRootPath);

            // Setting the ContentRootPath after setting a relative WebRootPath
            environment.ContentRootPath = tempPath;

            Assert.Equal(tempPath, environment.ContentRootPath);
            Assert.Equal(tempPath, ((PhysicalFileProvider)environment.ContentRootFileProvider).Root);
            Assert.Equal(Path.Combine(tempPath, webRootPath), environment.WebRootPath);
        }

        private class TestWebHostBuilder : IWebHostBuilder
        {
            private readonly Dictionary<string, string> _settings;
            private readonly IWebHostEnvironment _environment;

            public TestWebHostBuilder(Dictionary<string, string> settingsDictionary, IWebHostEnvironment environment)
            {
                _settings = settingsDictionary;
                _environment = environment;
            }

            public IWebHostEnvironment Environment { get; }

            public IWebHost Build()
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                var context = new WebHostBuilderContext
                {
                    HostingEnvironment = _environment,
                };

                configureDelegate(context, null!);

                return this;
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
