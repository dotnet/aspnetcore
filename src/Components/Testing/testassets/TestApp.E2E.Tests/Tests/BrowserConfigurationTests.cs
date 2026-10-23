// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright.TestAdapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestApp.E2E.Tests.Tests;

[UITest]
public partial class BrowserConfigurationTests : BrowserTest
{
    [TestMethod]
    public async Task ConfigurationIsProvidedByPlaywrightTestAdapter()
    {
        Assert.AreEqual(PlaywrightSettingsProvider.BrowserName, BrowserName);

        var originalBrowser = Environment.GetEnvironmentVariable("BROWSER");
        try
        {
            Environment.SetEnvironmentVariable(
                "BROWSER",
                BrowserName is "firefox" ? "chromium" : "firefox");
            var secondTestInstance = new BrowserTestProbe();

            await secondTestInstance.EnsureBrowserAsync();

            Assert.AreEqual(BrowserName, secondTestInstance.BrowserName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BROWSER", originalBrowser);
        }
    }

    private sealed class BrowserTestProbe : BrowserTest;
}
