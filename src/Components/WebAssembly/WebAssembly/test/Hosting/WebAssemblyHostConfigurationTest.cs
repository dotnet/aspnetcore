// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class WebAssemblyHostConfigurationTest
{
    [Fact]
    public void CanSetAndGetConfigurationValue()
    {
        // Arrange
        var initialData = new Dictionary<string, string>() {
                { "color", "blue" },
                { "type", "car" },
                { "wheels:year", "2008" },
                { "wheels:count", "4" },
                { "wheels:brand", "michelin" },
                { "wheels:brand:type", "rally" },
            };
        var memoryConfig = new MemoryConfigurationSource { InitialData = initialData };
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memoryConfig);
        configuration["type"] = "car";
        configuration["wheels:count"] = "6";

        // Assert
        Assert.Equal("car", configuration["type"]);
        Assert.Equal("blue", configuration["color"]);
        Assert.Equal("6", configuration["wheels:count"]);
    }

    [Fact]
    public void SettingValueUpdatesAllProviders()
    {
        // Arrange
        var initialData = new Dictionary<string, string>() { { "color", "blue" } };
        var source1 = new MemoryConfigurationSource { InitialData = initialData };
        var source2 = new CustomizedTestConfigurationSource();
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(source1);
        configuration.Add(source2);
        configuration["type"] = "car";

        // Assert
        Assert.Equal("car", configuration["type"]);
        IConfigurationRoot root = configuration;
        Assert.All(root.Providers, provider =>
        {
            provider.TryGet("type", out var value);
            Assert.Equal("car", value);
        });
    }

    [Fact]
    public void CanGetChildren()
    {
        // Arrange
        var initialData = new Dictionary<string, string>() { { "color", "blue" } };
        var memoryConfig = new MemoryConfigurationSource { InitialData = initialData };
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memoryConfig);
        IConfiguration readableConfig = configuration;
        var children = readableConfig.GetChildren();

        // Assert
        Assert.NotNull(children);
        Assert.NotEmpty(children);
    }

    [Fact]
    public void CanGetSection()
    {
        // Arrange
        var initialData = new Dictionary<string, string>() {
                { "color", "blue" },
                { "type", "car" },
                { "wheels:year", "2008" },
                { "wheels:count", "4" },
                { "wheels:brand", "michelin" },
                { "wheels:brand:type", "rally" },
            };
        var memoryConfig = new MemoryConfigurationSource { InitialData = initialData };
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memoryConfig);
        var section = configuration.GetSection("wheels").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);

        // Assert
        Assert.Equal(4, section.Count);
        Assert.Equal("2008", section["year"]);
        Assert.Equal("4", section["count"]);
        Assert.Equal("michelin", section["brand"]);
        Assert.Equal("rally", section["brand:type"]);
    }

    [Fact]
    public void CanDisposeProviders()
    {
        // Arrange
        var initialData = new Dictionary<string, string>() { { "color", "blue" } };
        var memoryConfig = new MemoryConfigurationSource { InitialData = initialData };
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memoryConfig);
        Assert.Equal("blue", configuration["color"]);
        var exception = Record.Exception(() => configuration.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void CanSupportDeeplyNestedConfigs()
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
        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memConfigSrc1);
        configuration.Add(memConfigSrc2);
        configuration.Add(memConfigSrc3);

        // Assert
        var dict = configuration.GetSection("Mem1").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
        Assert.Equal(3, dict.Count);
        Assert.Equal("NoKeyValue1", dict[""]);
        Assert.Equal("ValueInMem1", dict["KeyInMem1"]);
        Assert.Equal("ValueDeep1", dict["KeyInMem1:Deep1"]);

        var dict2 = configuration.GetSection("Mem2").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
        Assert.Equal(3, dict2.Count);
        Assert.Equal("NoKeyValue2", dict2[""]);
        Assert.Equal("ValueInMem2", dict2["KeyInMem2"]);
        Assert.Equal("ValueDeep2", dict2["KeyInMem2:Deep2"]);

        var dict3 = configuration.GetSection("Mem3").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
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

        var configuration = new WebAssemblyHostConfiguration();

        // Act
        configuration.Add(memConfigSrc1);
        configuration.Add(memConfigSrc2);

        // Assert
        Assert.Equal("ValueInMem2", configuration["Key1:Key2"]);
    }

    private class CustomizedTestConfigurationProvider : ConfigurationProvider
    {
        public CustomizedTestConfigurationProvider(string key, string value)
            => Data.Add(key, value.ToUpperInvariant());

        public override void Set(string key, string value)
        {
            Data[key] = value;
        }
    }

    private class CustomizedTestConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CustomizedTestConfigurationProvider("initialKey", "initialValue");
        }
    }
}
