// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

// For now this is limited to server-side execution because we don't have the ability to set the
// culture in client-side Blazor.
public class WebAssemblyGlobalizationTest : GlobalizationTest<ToggleExecutionModeServerFixture<Program>>
{
    public WebAssemblyGlobalizationTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void SetCulture(string culture)
    {
        Navigate($"{ServerPathBase}/?culture={culture}");

        // That should have triggered a page load, so wait for the main test selector to come up.
        Browser.MountTestComponent<GlobalizationBindCases>();
        Browser.Exists(By.Id("globalization-cases"));

        var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
        Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);
    }
}
