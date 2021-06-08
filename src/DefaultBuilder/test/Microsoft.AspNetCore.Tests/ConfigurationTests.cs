// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void AutoUpdatesByDefault()
        {
            var config = new Configuration();

            Assert.True(config.AutoUpdate);

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" },
            });

            Assert.Equal("TestValue", config["TestKey"]);
        }

        [Fact]
        public void AutoUpdateTriggersReloadTokenOnSourceModification()
        {
            var config = new Configuration();

            var reloadToken = ((IConfiguration)config).GetReloadToken();

            Assert.False(reloadToken.HasChanged);

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" },
            });

            Assert.True(reloadToken.HasChanged);
        }

        [Fact]
        public void DoesNotAutoUpdateWhenAutoUpdateDisabled()
        {
            var config = new Configuration
            {
                AutoUpdate = false,
            };

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" },
            });

            Assert.Null(config["TestKey"]);

            config.Update();

            Assert.Equal("TestValue", config["TestKey"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ManualUpdateTriggersReloadTokenWithOrWithoutAutoUpdate(bool autoUpdate)
        {
            var config = new Configuration
            {
                AutoUpdate = autoUpdate,
            };

            var manualReloadToken = ((IConfiguration)config).GetReloadToken();

            Assert.False(manualReloadToken.HasChanged);

            config.Update();

            Assert.True(manualReloadToken.HasChanged);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SettingValuesWorksWithOrWithoutAutoUpdate(bool autoUpdate)
        {
            var config = new Configuration
            {
                AutoUpdate = autoUpdate,
                ["TestKey"] = "TestValue",
            };

            Assert.Equal("TestValue", config["TestKey"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SettingValuesDoesNotTriggerReloadTokenWithOrWithoutAutoUpdate(bool autoUpdate)
        {
            var config = new Configuration
            {
                AutoUpdate = autoUpdate,
            };

            var reloadToken = ((IConfiguration)config).GetReloadToken();

            config["TestKey"] = "TestValue";

            Assert.Equal("TestValue", config["TestKey"]);

            // ConfigurationRoot doesn't fire the token today when the setter is called. Maybe we should change that.
            // At least you can manually call Configuration.Update() to fire a reload though this reloads all sources unnecessarily.
            Assert.False(reloadToken.HasChanged);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SettingIConfigurationBuilderPropertiesWorksWithoutAutoUpdate(bool autoUpdate)
        {
            var config = new Configuration
            {
                AutoUpdate = autoUpdate,
            };

            var configBuilder = (IConfigurationBuilder)config;

            var reloadToken = ((IConfiguration)config).GetReloadToken();

            configBuilder.Properties["TestKey"] = "TestValue";

            Assert.Equal("TestValue", configBuilder.Properties["TestKey"]);

            // Changing properties should not change config keys or fire reload token.
            Assert.Null(config["TestKey"]);
            Assert.False(reloadToken.HasChanged);
        }
    }
}
