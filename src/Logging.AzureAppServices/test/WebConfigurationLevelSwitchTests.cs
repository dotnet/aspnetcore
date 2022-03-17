// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class WebConfigurationLevelSwitchTests
{
    [Theory]
    [InlineData("Error", LogLevel.Error)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Information", LogLevel.Information)]
    [InlineData("Verbose", LogLevel.Trace)]
    [InlineData("ABCD", LogLevel.None)]
    public void AddsRuleWithCorrectLevel(string levelValue, LogLevel expectedLevel)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new[]
            {
                    new KeyValuePair<string, string>("levelKey", levelValue),
            })
            .Build();

        var levelSwitcher = new ConfigurationBasedLevelSwitcher(configuration, typeof(TestFileLoggerProvider), "levelKey");

        var filterConfiguration = new LoggerFilterOptions();
        levelSwitcher.Configure(filterConfiguration);

        Assert.Equal(1, filterConfiguration.Rules.Count);

        var rule = filterConfiguration.Rules[0];
        Assert.Equal(typeof(TestFileLoggerProvider).FullName, rule.ProviderName);
        Assert.Equal(expectedLevel, rule.LogLevel);
    }
}
