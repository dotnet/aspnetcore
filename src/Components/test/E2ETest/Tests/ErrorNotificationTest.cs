// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

[Collection("ErrorNotification")] // When the clientside and serverside tests run together it seems to cause failures, possibly due to connection lose on exception.
public class ErrorNotificationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public ErrorNotificationTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // On WebAssembly, page reloads are expensive so skip if possible
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ErrorComponent>();
        Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Exists(By.Id("throw-simple-exception"));
    }

    [Fact]
    public void ShowsErrorNotification_OnError_Dismiss()
    {
        var errorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Assert.Equal("none", errorUi.GetCssValue("display"));

        var causeErrorButton = Browser.Exists(By.Id("throw-simple-exception"));
        causeErrorButton.Click();

        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));

        var reload = Browser.Exists(By.ClassName("reload"));
        reload.Click();

        Browser.DoesNotExist(By.TagName("button"));
    }

    [Fact]
    public void ShowsErrorNotification_OnError_Reload()
    {
        var causeErrorButton = Browser.Exists(By.Id("throw-simple-exception"));
        var errorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Assert.Equal("none", errorUi.GetCssValue("display"));

        causeErrorButton.Click();
        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

        var dismiss = Browser.Exists(By.ClassName("dismiss"));
        dismiss.Click();
        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: none;']"));
    }
}
