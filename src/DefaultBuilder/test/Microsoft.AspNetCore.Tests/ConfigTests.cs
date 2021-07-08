// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void AutoUpdates()
        {
            var config = new Config();

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" },
            });

            Assert.Equal("TestValue", config["TestKey"]);
        }

        [Fact]
        public void TriggersReloadTokenOnSourceAddition()
        {
            var config = new Config();

            var reloadToken = ((IConfiguration)config).GetReloadToken();

            Assert.False(reloadToken.HasChanged);

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" },
            });

            Assert.True(reloadToken.HasChanged);
        }


        [Fact]
        public void SettingValuesWorksWithoutManuallyAddingSource()
        {
            var config = new Config
            {
                ["TestKey"] = "TestValue",
            };

            Assert.Equal("TestValue", config["TestKey"]);
        }

        [Fact]
        public void SettingConfigValuesDoesNotTriggerReloadToken()
        {
            var config = new Config();
            var reloadToken = ((IConfiguration)config).GetReloadToken();

            config["TestKey"] = "TestValue";

            Assert.Equal("TestValue", config["TestKey"]);

            // ConfigurationRoot doesn't fire the token today when the setter is called. Maybe we should change that.
            Assert.False(reloadToken.HasChanged);
        }

        [Fact]
        public void SettingIConfigurationBuilderPropertiesReloadsSources()
        {
            var config = new Config();
            IConfigurationBuilder configBuilder = config;

            config["PreReloadTestConfigKey"] = "PreReloadTestConfigValue";

            var reloadToken1 = ((IConfiguration)config).GetReloadToken();
            // Changing Properties causes all the IConfigurationSources to be reload.
            configBuilder.Properties["TestPropertyKey"] = "TestPropertyValue";

            var reloadToken2 = ((IConfiguration)config).GetReloadToken();
            config["PostReloadTestConfigKey"] = "PostReloadTestConfigValue";

            Assert.Equal("TestPropertyValue", configBuilder.Properties["TestPropertyKey"]);
            Assert.Null(config["TestPropertyKey"]);

            // Changes before the reload are lost by the MemoryConfigurationSource.
            Assert.Null(config["PreReloadTestConfigKey"]);
            Assert.Equal("PostReloadTestConfigValue", config["PostReloadTestConfigKey"]);

            Assert.True(reloadToken1.HasChanged);
            Assert.False(reloadToken2.HasChanged);
        }

        [Fact]
        public void DisposesProvidersOnDispose()
        {
            var provider1 = new TestConfigurationProvider("foo", "foo-value");
            var provider2 = new DisposableTestConfigurationProvider("bar", "bar-value");
            var provider3 = new TestConfigurationProvider("baz", "baz-value");
            var provider4 = new DisposableTestConfigurationProvider("qux", "qux-value");
            var provider5 = new DisposableTestConfigurationProvider("quux", "quux-value");

            var config = new Config();
            IConfigurationBuilder builder = config;

            builder.Add(new TestConfigurationSource(provider1));
            builder.Add(new TestConfigurationSource(provider2));
            builder.Add(new TestConfigurationSource(provider3));
            builder.Add(new TestConfigurationSource(provider4));
            builder.Add(new TestConfigurationSource(provider5));

            Assert.Equal("foo-value", config["foo"]);
            Assert.Equal("bar-value", config["bar"]);
            Assert.Equal("baz-value", config["baz"]);
            Assert.Equal("qux-value", config["qux"]);
            Assert.Equal("quux-value", config["quux"]);

            config.Dispose();

            Assert.True(provider2.IsDisposed);
            Assert.True(provider4.IsDisposed);
            Assert.True(provider5.IsDisposed);
        }

        [Fact]
        public void DisposesProvidersOnRemoval()
        {
            var provider1 = new TestConfigurationProvider("foo", "foo-value");
            var provider2 = new DisposableTestConfigurationProvider("bar", "bar-value");
            var provider3 = new TestConfigurationProvider("baz", "baz-value");
            var provider4 = new DisposableTestConfigurationProvider("qux", "qux-value");
            var provider5 = new DisposableTestConfigurationProvider("quux", "quux-value");

            var source1 = new TestConfigurationSource(provider1);
            var source2 = new TestConfigurationSource(provider2);
            var source3 = new TestConfigurationSource(provider3);
            var source4 = new TestConfigurationSource(provider4);
            var source5 = new TestConfigurationSource(provider5);

            var config = new Config();
            IConfigurationBuilder builder = config;

            builder.Add(source1);
            builder.Add(source2);
            builder.Add(source3);
            builder.Add(source4);
            builder.Add(source5);

            Assert.Equal("foo-value", config["foo"]);
            Assert.Equal("bar-value", config["bar"]);
            Assert.Equal("baz-value", config["baz"]);
            Assert.Equal("qux-value", config["qux"]);
            Assert.Equal("quux-value", config["quux"]);

            builder.Sources.Remove(source2);
            builder.Sources.Remove(source4);

            // While only provider2 and provider4 need to be disposed here, we do not assert provider5 is not disposed
            // because even though it's unnecessary, Configuration disposes all providers on removal and rebuilds
            // all the sources. While not optimal, this should be a pretty rare scenario.
            Assert.True(provider2.IsDisposed);
            Assert.True(provider4.IsDisposed);

            config.Dispose();

            Assert.True(provider2.IsDisposed);
            Assert.True(provider4.IsDisposed);
            Assert.True(provider5.IsDisposed);
        }

        [Fact]
        public void DisposesChangeTokenRegistrationsOnDispose()
        {
            var changeToken = new TestChangeToken();
            var providerMock = new Mock<IConfigurationProvider>();
            providerMock.Setup(p => p.GetReloadToken()).Returns(changeToken);

            var config = new Config();

            ((IConfigurationBuilder)config).Add(new TestConfigurationSource(providerMock.Object));

            Assert.NotEmpty(changeToken.Callbacks);

            config.Dispose();

            Assert.Empty(changeToken.Callbacks);
        }

        [Fact]
        public void DisposesChangeTokenRegistrationsOnRemoval()
        {
            var changeToken = new TestChangeToken();
            var providerMock = new Mock<IConfigurationProvider>();
            providerMock.Setup(p => p.GetReloadToken()).Returns(changeToken);

            var source = new TestConfigurationSource(providerMock.Object);

            var config = new Config();
            IConfigurationBuilder builder = config;

            builder.Add(source);

            Assert.NotEmpty(changeToken.Callbacks);

            builder.Sources.Remove(source);

            Assert.Empty(changeToken.Callbacks);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ChainedConfigurationIsDisposedOnDispose(bool shouldDispose)
        {
            var provider = new DisposableTestConfigurationProvider("foo", "foo-value");
            var chainedConfig = new ConfigurationRoot(new IConfigurationProvider[] {
                provider
            });

            var config = new Config();

            config.AddConfiguration(chainedConfig, shouldDisposeConfiguration: shouldDispose);

            Assert.False(provider.IsDisposed);

            config.Dispose();

            Assert.Equal(shouldDispose, provider.IsDisposed);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProviders()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "ValueInMem2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3:KeyInMem3", "ValueInMem3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var memVal1 = config["mem1:keyinmem1"];
            var memVal2 = config["Mem2:KeyInMem2"];
            var memVal3 = config["MEM3:KEYINMEM3"];

            // Assert
            Assert.Contains(memConfigSrc1, configurationBuilder.Sources);
            Assert.Contains(memConfigSrc2, configurationBuilder.Sources);
            Assert.Contains(memConfigSrc3, configurationBuilder.Sources);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Equal("ValueInMem1", config["mem1:keyinmem1"]);
            Assert.Equal("ValueInMem2", config["Mem2:KeyInMem2"]);
            Assert.Equal("ValueInMem3", config["MEM3:KEYINMEM3"]);
            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void CanChainConfiguration()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "ValueInMem2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3:KeyInMem3", "ValueInMem3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var chained = new ConfigurationBuilder().AddConfiguration(config).Build();
            var memVal1 = chained["mem1:keyinmem1"];
            var memVal2 = chained["Mem2:KeyInMem2"];
            var memVal3 = chained["MEM3:KEYINMEM3"];

            // Assert

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Null(chained["NotExist"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChainedAsEnumerateFlattensIntoDictionaryTest(bool removePath)
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config1 = new Config();
            IConfigurationBuilder configurationBuilder = config1;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);

            var config2 = new Config();

            config2
                .AddConfiguration(config1)
                .Add(memConfigSrc3);

            var dict = config2.AsEnumerable(makePathsRelative: removePath).ToDictionary(k => k.Key, v => v.Value);

            // Assert
            Assert.Equal("Value1", dict["Mem1"]);
            Assert.Equal("NoKeyValue1", dict["Mem1:"]);
            Assert.Equal("ValueDeep1", dict["Mem1:KeyInMem1:Deep1"]);
            Assert.Equal("ValueInMem2", dict["Mem2:KeyInMem2"]);
            Assert.Equal("Value2", dict["Mem2"]);
            Assert.Equal("NoKeyValue2", dict["Mem2:"]);
            Assert.Equal("ValueDeep2", dict["Mem2:KeyInMem2:Deep2"]);
            Assert.Equal("Value3", dict["Mem3"]);
            Assert.Equal("NoKeyValue3", dict["Mem3:"]);
            Assert.Equal("ValueInMem3", dict["Mem3:KeyInMem3"]);
            Assert.Equal("ValueDeep3", dict["Mem3:KeyInMem3:Deep3"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AsEnumerateFlattensIntoDictionaryTest(bool removePath)
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);
            var dict = config.AsEnumerable(makePathsRelative: removePath).ToDictionary(k => k.Key, v => v.Value);

            // Assert
            Assert.Equal("Value1", dict["Mem1"]);
            Assert.Equal("NoKeyValue1", dict["Mem1:"]);
            Assert.Equal("ValueDeep1", dict["Mem1:KeyInMem1:Deep1"]);
            Assert.Equal("ValueInMem2", dict["Mem2:KeyInMem2"]);
            Assert.Equal("Value2", dict["Mem2"]);
            Assert.Equal("NoKeyValue2", dict["Mem2:"]);
            Assert.Equal("ValueDeep2", dict["Mem2:KeyInMem2:Deep2"]);
            Assert.Equal("Value3", dict["Mem3"]);
            Assert.Equal("NoKeyValue3", dict["Mem3:"]);
            Assert.Equal("ValueInMem3", dict["Mem3:KeyInMem3"]);
            Assert.Equal("ValueDeep3", dict["Mem3:KeyInMem3:Deep3"]);
        }

        [Fact]
        public void AsEnumerateStripsKeyFromChildren()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem4", "ValueInMem4"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"},
                {"Mem3:KeyInMem3:Deep4", "ValueDeep4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var dict = config.GetSection("Mem1").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(3, dict.Count);
            Assert.Equal("NoKeyValue1", dict[""]);
            Assert.Equal("ValueInMem1", dict["KeyInMem1"]);
            Assert.Equal("ValueDeep1", dict["KeyInMem1:Deep1"]);

            var dict2 = config.GetSection("Mem2").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(3, dict2.Count);
            Assert.Equal("NoKeyValue2", dict2[""]);
            Assert.Equal("ValueInMem2", dict2["KeyInMem2"]);
            Assert.Equal("ValueDeep2", dict2["KeyInMem2:Deep2"]);

            var dict3 = config.GetSection("Mem3").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(5, dict3.Count);
            Assert.Equal("NoKeyValue3", dict3[""]);
            Assert.Equal("ValueInMem3", dict3["KeyInMem3"]);
            Assert.Equal("ValueInMem4", dict3["KeyInMem4"]);
            Assert.Equal("ValueDeep3", dict3["KeyInMem3:Deep3"]);
            Assert.Equal("ValueDeep4", dict3["KeyInMem3:Deep4"]);
        }

        [Fact]
        public void NewConfigurationProviderOverridesOldOneWhenKeyIsDuplicated()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem1"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem2"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);

            // Assert
            Assert.Equal("ValueInMem2", config["Key1:Key2"]);
        }

        [Fact]
        public void NewConfigurationRootMayBeBuiltFromExistingWithDuplicateKeys()
        {
            var configurationRoot = new ConfigurationBuilder()
                                    .AddInMemoryCollection(new Dictionary<string, string>
                                        {
                                            {"keya:keyb", "valueA"},
                                        })
                                    .AddInMemoryCollection(new Dictionary<string, string>
                                        {
                                            {"KEYA:KEYB", "valueB"}
                                        })
                                    .Build();
            var newConfigurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationRoot.AsEnumerable())
                .Build();
            Assert.Equal("valueB", newConfigurationRoot["keya:keyb"]);
        }

        [Fact]
        public void SettingValueUpdatesAllConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"}
            };

            var memConfigSrc1 = new TestMemorySourceProvider(dict);
            var memConfigSrc2 = new TestMemorySourceProvider(dict);
            var memConfigSrc3 = new TestMemorySourceProvider(dict);

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            // Act
            config["Key1"] = "NewValue1";
            config["Key2"] = "NewValue2";

            var memConfigProvider1 = memConfigSrc1.Build(configurationBuilder);
            var memConfigProvider2 = memConfigSrc2.Build(configurationBuilder);
            var memConfigProvider3 = memConfigSrc3.Build(configurationBuilder);

            // Assert
            Assert.Equal("NewValue1", config["Key1"]);
            Assert.Equal("NewValue1", Get(memConfigProvider1, "Key1"));
            Assert.Equal("NewValue1", Get(memConfigProvider2, "Key1"));
            Assert.Equal("NewValue1", Get(memConfigProvider3, "Key1"));
            Assert.Equal("NewValue2", config["Key2"]);
            Assert.Equal("NewValue2", Get(memConfigProvider1, "Key2"));
            Assert.Equal("NewValue2", Get(memConfigProvider2, "Key2"));
            Assert.Equal("NewValue2", Get(memConfigProvider3, "Key2"));
        }

        [Fact]
        public void CanGetConfigurationSection()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Data:DB1:Connection1", "MemVal1"},
                {"Data:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"DataSource:DB2:Connection", "MemVal3"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Data", "MemVal4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            // Act
            var configFocus = config.GetSection("Data");

            var memVal1 = configFocus["DB1:Connection1"];
            var memVal2 = configFocus["DB1:Connection2"];
            var memVal3 = configFocus["DB2:Connection"];
            var memVal4 = configFocus["Source:DB2:Connection"];
            var memVal5 = configFocus.Value;

            // Assert
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal4", memVal5);

            Assert.Equal("MemVal1", configFocus["DB1:Connection1"]);
            Assert.Equal("MemVal2", configFocus["DB1:Connection2"]);
            Assert.Null(configFocus["DB2:Connection"]);
            Assert.Null(configFocus["Source:DB2:Connection"]);
            Assert.Equal("MemVal4", configFocus.Value);
        }

        [Fact]
        public void CanGetConnectionStrings()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"ConnectionStrings:DB1:Connection1", "MemVal1"},
                {"ConnectionStrings:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"ConnectionStrings:DB2:Connection", "MemVal3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);

            // Act
            var memVal1 = config.GetConnectionString("DB1:Connection1");
            var memVal2 = config.GetConnectionString("DB1:Connection2");
            var memVal3 = config.GetConnectionString("DB2:Connection");

            // Assert
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal3", memVal3);
        }

        [Fact]
        public void CanGetConfigurationChildren()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Data:DB1:Connection1", "MemVal1"},
                {"Data:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Data:DB2Connection", "MemVal3"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"DataSource:DB3:Connection", "MemVal4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            // Act
            var configSections = config.GetSection("Data").GetChildren().ToList();

            // Assert
            Assert.Equal(2, configSections.Count());
            Assert.Equal("MemVal1", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection1"]);
            Assert.Equal("MemVal2", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection2"]);
            Assert.Equal("MemVal3", configSections.FirstOrDefault(c => c.Key == "DB2Connection").Value);
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
        }

        [Fact]
        public void SourcesReturnsAddedConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem:KeyInMem", "MemVal"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dict };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dict };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dict };

            var config = new Config();
            IConfigurationBuilder configurationBuilder = config;

            // Act

            // A MemoryConfigurationSource is added by default, so there will be no error unless we clear it
            configurationBuilder.Sources.Clear();
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            // Assert
            Assert.Equal(new[] { memConfigSrc1, memConfigSrc2, memConfigSrc3 }, configurationBuilder.Sources);
        }

        [Fact]
        public void SetValueThrowsExceptionNoSourceRegistered()
        {
            // Arrange
            var config = new Config();

            // A MemoryConfigurationSource is added by default, so there will be no error unless we clear it
            config["Title"] = "Welcome";

            ((IConfigurationBuilder)config).Sources.Clear();

            var expectedMsg = "A configuration source is not registered. Please register one before setting a value.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => config["Title"] = "Welcome");

            // Assert
            Assert.Equal(expectedMsg, ex.Message);
        }

        [Fact]
        public void SameReloadTokenIsReturnedRepeatedly()
        {
            // Arrange
            IConfiguration config = new Config();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
        }

        [Fact]
        public void DifferentReloadTokenReturnedAfterReloading()
        {
            // Arrange
            IConfigurationRoot config = new Config();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();
            config.Reload();
            var token3 = config.GetReloadToken();
            var token4 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
            Assert.Same(token3, token4);
            Assert.NotSame(token1, token3);
        }

        [Fact]
        public void TokenTriggeredWhenReloadOccurs()
        {
            // Arrange
            IConfigurationRoot config = new Config();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;

            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
        }

        [Fact]
        public void MultipleCallbacksCanBeRegisteredToReload()
        {
            // Arrange
            IConfigurationRoot config = new Config();

            // Act
            var token1 = config.GetReloadToken();
            var called1 = 0;
            token1.RegisterChangeCallback(_ => called1++, state: null);
            var called2 = 0;
            token1.RegisterChangeCallback(_ => called2++, state: null);

            // Assert
            Assert.Equal(0, called1);
            Assert.Equal(0, called2);

            config.Reload();
            Assert.Equal(1, called1);
            Assert.Equal(1, called2);

            var token2 = config.GetReloadToken();
            var cleanup1 = token2.RegisterChangeCallback(_ => called1++, state: null);
            token2.RegisterChangeCallback(_ => called2++, state: null);

            cleanup1.Dispose();

            config.Reload();
            Assert.Equal(1, called1);
            Assert.Equal(2, called2);
        }

        [Fact]
        public void NewTokenAfterReloadIsNotChanged()
        {
            // Arrange
            IConfigurationRoot config = new Config();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;
            var token2 = config.GetReloadToken();
            var hasChanged3 = token2.HasChanged;

            // 
            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
            Assert.False(hasChanged3);
            Assert.NotSame(token1, token2);
        }

        [Fact]
        public void KeyStartingWithColonMeansFirstSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                [":Key2"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal(string.Empty, children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal("Key2", children.First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyWithDoubleColonHasSectionWithEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1::Key3"] = "value"
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal("Key1", children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
            Assert.Single(children.First().GetChildren().First().GetChildren());
            Assert.Equal("Key3", children.First().GetChildren().First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyEndingWithColonMeansLastSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1:"] = "value"
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal("Key1", children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
        }

        [Fact]
        public void SectionWithValueExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sectionExists1 = config.GetSection("Mem1").Exists();
            var sectionExists2 = config.GetSection("Mem1:KeyInMem1").Exists();
            var sectionNotExists = config.GetSection("Mem2").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
            Assert.False(sectionNotExists);
        }

        [Fact]
        public void SectionGetRequiredSectionSuccess()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sectionExists1 = config.GetRequiredSection("Mem1").Exists();
            var sectionExists2 = config.GetRequiredSection("Mem1:KeyInMem1").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
        }

        [Fact]
        public void SectionGetRequiredSectionMissingThrowException()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:Deep1", "Value1"},
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            Assert.Throws<InvalidOperationException>(() => config.GetRequiredSection("Mem2"));
            Assert.Throws<InvalidOperationException>(() => config.GetRequiredSection("Mem1:Deep2"));
        }

        [Fact]
        public void SectionWithChildrenExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"},
                {"Mem2:KeyInMem2:Deep1", "ValueDeep2"}
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sectionExists1 = config.GetSection("Mem1").Exists();
            var sectionExists2 = config.GetSection("Mem2").Exists();
            var sectionNotExists = config.GetSection("Mem3").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
            Assert.False(sectionNotExists);
        }

        [Theory]
        [InlineData("Value1")]
        [InlineData("")]
        public void KeyWithValueAndWithoutChildrenExistsAsSection(string value)
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", value}
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sectionExists = config.GetSection("Mem1").Exists();

            // Assert
            Assert.True(sectionExists);
        }

        [Fact]
        public void KeyWithNullValueAndWithoutChildrenIsASectionButNotExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", null}
            };

            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sections = config.GetChildren();
            var sectionExists = config.GetSection("Mem1").Exists();
            var sectionChildren = config.GetSection("Mem1").GetChildren();

            // Assert
            Assert.Single(sections, section => section.Key == "Mem1");
            Assert.False(sectionExists);
            Assert.Empty(sectionChildren);
        }

        [Fact]
        public void SectionWithChildrenHasNullValue()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"},
            };


            var config = new Config();
            ((IConfigurationBuilder)config).AddInMemoryCollection(dict);

            // Act
            var sectionValue = config.GetSection("Mem1").Value;

            // Assert
            Assert.Null(sectionValue);
        }

        [Fact]
        public void ProviderWithNullReloadToken()
        {
            // Arrange
            var config = new Config();
            IConfigurationBuilder builder = config;

            // Assert
            Assert.NotNull(builder.Build());
        }

        [Fact]
        public void BuildReturnsThis()
        {
            // Arrange
            var config = new Config();

            // Assert
            Assert.Same(config, ((IConfigurationBuilder)config).Build());
        }

        private static string Get(IConfigurationProvider provider, string key)
        {
            string value;

            if (!provider.TryGet(key, out value))
            {
                throw new InvalidOperationException("Key not found");
            }

            return value;
        }

        private class TestConfigurationSource : IConfigurationSource
        {
            private readonly IConfigurationProvider _provider;

            public TestConfigurationSource(IConfigurationProvider provider)
            {
                _provider = provider;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return _provider;
            }
        }

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }

        private class DisposableTestConfigurationProvider : ConfigurationProvider, IDisposable
        {
            public bool IsDisposed { get; set; }

            public DisposableTestConfigurationProvider(string key, string value)
                => Data.Add(key, value);

            public void Dispose()
                => IsDisposed = true;
        }

        private class TestChangeToken : IChangeToken
        {
            public List<(Action<object>, object)> Callbacks { get; } = new List<(Action<object>, object)>();

            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => true;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                var item = (callback, state);
                Callbacks.Add(item);
                return new DisposableAction(() => Callbacks.Remove(item));
            }

            private class DisposableAction : IDisposable
            {
                private Action _action;

                public DisposableAction(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    var a = _action;
                    if (a != null)
                    {
                        _action = null;
                        a();
                    }
                }
            }
        }

        private class TestMemorySourceProvider : MemoryConfigurationProvider, IConfigurationSource
        {
            public TestMemorySourceProvider(Dictionary<string, string> initialData)
                : base(new MemoryConfigurationSource { InitialData = initialData })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return this;
            }
        }

        private class NullReloadTokenConfigSource : IConfigurationSource, IConfigurationProvider
        {
            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) => throw new NotImplementedException();
            public IChangeToken GetReloadToken() => null;
            public void Load() { }
            public void Set(string key, string value) => throw new NotImplementedException();
            public bool TryGet(string key, out string value) => throw new NotImplementedException();
            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
        }

    }
}
