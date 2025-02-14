// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class StartupErrorNotificationTest : ServerTestBase<BlazorWasmTestAppFixture<Program>>
{
    public StartupErrorNotificationTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = ServerPathBase;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisplaysNotificationForStartupException(bool errorIsAsync)
    {
        var url = $"{ServerPathBase}?error={(errorIsAsync ? "async" : "sync")}";

        Navigate(url);
        var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
        Assert.NotNull(errorUiElem);

        Browser.Equal("block", () => errorUiElem.GetCssValue("display"));
    }
}
