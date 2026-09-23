// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright.TestAdapter;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

[Collection(nameof(PlaywrightSettingsProviderTests))]
public class PlaywrightSettingsProviderTests
{
    [Fact]
    public void EnvironmentVariablesConfigureBrowserAndHeadedMode()
    {
        var originalBrowser = Environment.GetEnvironmentVariable("BROWSER");
        var originalHeaded = Environment.GetEnvironmentVariable("HEADED");
        try
        {
            Environment.SetEnvironmentVariable("BROWSER", "FIREFOX");
            Environment.SetEnvironmentVariable("HEADED", "1");

            Assert.Equal("firefox", PlaywrightSettingsProvider.BrowserName);
            Assert.False(PlaywrightSettingsProvider.LaunchOptions.Headless);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BROWSER", originalBrowser);
            Environment.SetEnvironmentVariable("HEADED", originalHeaded);
        }
    }
}

[CollectionDefinition(nameof(PlaywrightSettingsProviderTests), DisableParallelization = true)]
public class PlaywrightSettingsProviderTestCollection;
