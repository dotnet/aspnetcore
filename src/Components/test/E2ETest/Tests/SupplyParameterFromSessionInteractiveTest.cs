// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class SupplyParameterFromInteractiveTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public SupplyParameterFromInteractiveTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
    }

    [Fact]
    public void SupplyParameterFromSessionDoesNotWorkInInteractiveMode()
    {
        Browser.MountTestComponent<SupplyParameterFromSessionInteractive>();
        Browser.Exists(By.TagName("h3"));
        Browser.Equal("(empty)", () => Browser.Exists(By.Id("session-value")).Text);
    }

    [Fact]
    public void SupplyParameterFromTempDataDoesNotWorkInInteractiveMode()
    {
        Browser.MountTestComponent<SupplyParameterFromSessionInteractive>();
        Browser.Exists(By.TagName("h3"));
        Browser.Equal("(empty)", () => Browser.Exists(By.Id("tempdata-value")).Text);
    }
}
