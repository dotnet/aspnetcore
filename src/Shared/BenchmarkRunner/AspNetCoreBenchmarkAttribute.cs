// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
internal class AspNetCoreBenchmarkAttribute : Attribute, IConfigSource
{
    public AspNetCoreBenchmarkAttribute()
        : this(typeof(DefaultCoreConfig))
    {
    }

    public AspNetCoreBenchmarkAttribute(Type configType)
        : this(configType, typeof(DefaultCoreValidationConfig))
    {
    }

    public AspNetCoreBenchmarkAttribute(Type configType, Type validationConfigType)
    {
        ConfigTypes = new Dictionary<string, Type>()
            {
                { NamedConfiguration.Default, typeof(DefaultCoreConfig) },
                { NamedConfiguration.Validation, typeof(DefaultCoreValidationConfig) },
                { NamedConfiguration.Profile, typeof(DefaultCoreProfileConfig) },
                { NamedConfiguration.Debug, typeof(DefaultCoreDebugConfig) },
                { NamedConfiguration.PerfLab, typeof(DefaultCorePerfLabConfig) },
            };

        if (configType != null)
        {
            ConfigTypes[NamedConfiguration.Default] = configType;
        }

        if (validationConfigType != null)
        {
            ConfigTypes[NamedConfiguration.Validation] = validationConfigType;
        }
    }

    public IConfig Config
    {
        get
        {
            if (!ConfigTypes.TryGetValue(ConfigName ?? NamedConfiguration.Default, out var configType))
            {
                var message = $"Could not find a configuration matching {ConfigName}. " +
                    $"Known configurations: {string.Join(", ", ConfigTypes.Keys)}";
                throw new InvalidOperationException(message);
            }

            return (IConfig)Activator.CreateInstance(configType, Array.Empty<object>());
        }
    }

    public Dictionary<string, Type> ConfigTypes { get; }

    public static string ConfigName { get; set; } = NamedConfiguration.Default;

    public static class NamedConfiguration
    {
        public const string Default = "default";
        public const string Validation = "validation";
        public const string Profile = "profile";
        public const string Debug = "debug";
        public const string PerfLab = "perflab";
    }
}
