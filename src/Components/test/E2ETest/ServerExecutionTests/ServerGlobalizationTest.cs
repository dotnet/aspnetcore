// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

// For now this is limited to server-side execution because we don't have the ability to set the
// culture in client-side Blazor.
public class ServerGlobalizationTest : GlobalizationTest<BasicTestAppServerSiteFixture<InternationalizationStartup>>
{
    public ServerGlobalizationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<InternationalizationStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<CulturePicker>();
        Browser.Exists(By.Id("culture-selector"));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public override void CanSetCultureAndParseCultureSensitiveNumbersAndDates(string culture)
    {
        base.CanSetCultureAndParseCultureSensitiveNumbersAndDates(culture);
    }

    protected override void SetCulture(string culture)
    {
        var selector = new SelectElement(Browser.Exists(By.Id("culture-selector")));
        selector.SelectByValue(culture);

        // Click the link to return back to the test page
        Browser.Exists(By.ClassName("return-from-culture-setter")).Click();

        // That should have triggered a page load, so wait for the main test selector to come up.
        Browser.MountTestComponent<GlobalizationBindCases>();
        Browser.Exists(By.Id("globalization-cases"));

        var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
        Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);
    }
}
