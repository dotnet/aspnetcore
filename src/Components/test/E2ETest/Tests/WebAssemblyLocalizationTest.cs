// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyLocalizationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public WebAssemblyLocalizationTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData("en-US", "Hello!")]
    [InlineData("fr-FR", "Bonjour!")]
    public void CanSetCultureAndReadLocalizedResources(string culture, string message)
    {
        Navigate($"{ServerPathBase}/?culture={culture}");

        Browser.MountTestComponent<LocalizedText>();

        var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
        Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

        var messageDisplay = Browser.Exists(By.Id("message-display"));
        Assert.Equal(message, messageDisplay.Text);
    }
}
