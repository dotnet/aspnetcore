// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerLocalizationTest : ServerTestBase<BasicTestAppServerSiteFixture<InternationalizationStartup>>
{
    public ServerLocalizationTest(
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
    [InlineData("en-US", "Hello!")]
    [InlineData("fr-FR", "Bonjour!")]
    public void CanSetCultureAndReadLocalizedResources(string culture, string message)
    {
        var selector = new SelectElement(Browser.Exists(By.Id("culture-selector")));
        selector.SelectByValue(culture);

        // Click the link to return back to the test page
        Browser.Exists(By.ClassName("return-from-culture-setter")).Click();

        // That should have triggered a page load, so wait for the main test selector to come up.
        Browser.MountTestComponent<LocalizedText>();

        var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
        Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

        var messageDisplay = Browser.Exists(By.Id("message-display"));
        Assert.Equal(message, messageDisplay.Text);
    }
}
