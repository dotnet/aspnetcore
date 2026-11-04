// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices;

internal sealed class ConfigurationBasedLevelSwitcher : IConfigureOptions<LoggerFilterOptions>
{
    private readonly IConfiguration _configuration;
    private readonly Type _provider;
    private readonly string _levelKey;

    public ConfigurationBasedLevelSwitcher(IConfiguration configuration, Type provider, string levelKey)
    {
        _configuration = configuration;
        _provider = provider;
        _levelKey = levelKey;
    }

    public void Configure(LoggerFilterOptions options)
    {
        options.Rules.Add(new LoggerFilterRule(_provider.FullName, null, GetLogLevel(), null));
    }

    private LogLevel GetLogLevel()
    {
        return TextToLogLevel(_configuration.GetSection(_levelKey)?.Value);
    }

    private static LogLevel TextToLogLevel(string text)
    {
        switch (text?.ToUpperInvariant())
        {
            case "ERROR":
                return LogLevel.Error;
            case "WARNING":
                return LogLevel.Warning;
            case "INFORMATION":
                return LogLevel.Information;
            case "VERBOSE":
                return LogLevel.Trace;
            default:
                return LogLevel.None;
        }
    }
}
