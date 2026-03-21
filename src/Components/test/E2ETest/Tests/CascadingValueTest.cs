// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class CascadingValueTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public CascadingValueTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<BasicTestApp.CascadingValueTest.CascadingValueSupplier>();
    }

    [Fact]
    public void CanUpdateValuesMatchedByType()
    {
        var currentCount = Browser.Exists(By.Id("current-count"));
        var incrementButton = Browser.Exists(By.Id("increment-count"));

        // We have the correct initial value
        Browser.Equal("100", () => currentCount.Text);

        // Updates are propagated
        incrementButton.Click();
        Browser.Equal("101", () => currentCount.Text);
        incrementButton.Click();
        Browser.Equal("102", () => currentCount.Text);

        // Didn't re-render unrelated descendants
        Assert.Equal("1", Browser.Exists(By.Id("receive-by-interface-num-renders")).Text);
    }

    [Fact]
    public void CanUpdateValuesMatchedByName()
    {
        var currentFlag1Value = Browser.Exists(By.Id("flag-1"));
        var currentFlag2Value = Browser.Exists(By.Id("flag-2"));

        Browser.Equal("False", () => currentFlag1Value.Text);
        Browser.Equal("False", () => currentFlag2Value.Text);

        // Observe that the correct cascading parameter updates
        Browser.Exists(By.Id("toggle-flag-1")).Click();
        Browser.Equal("True", () => currentFlag1Value.Text);
        Browser.Equal("False", () => currentFlag2Value.Text);
        Browser.Exists(By.Id("toggle-flag-2")).Click();
        Browser.Equal("True", () => currentFlag1Value.Text);
        Browser.Equal("True", () => currentFlag2Value.Text);

        // Didn't re-render unrelated descendants
        Assert.Equal("1", Browser.Exists(By.Id("receive-by-interface-num-renders")).Text);
    }

    [Fact]
    public void CanUpdateFixedValuesMatchedByInterface()
    {
        var currentCount = Browser.Exists(By.Id("current-count"));
        var decrementButton = Browser.Exists(By.Id("decrement-count"));

        // We have the correct initial value
        Browser.Equal("100", () => currentCount.Text);

        // Updates are propagated
        decrementButton.Click();
        Browser.Equal("99", () => currentCount.Text);
        decrementButton.Click();
        Browser.Equal("98", () => currentCount.Text);

        // Didn't re-render descendants
        Assert.Equal("1", Browser.Exists(By.Id("receive-by-interface-num-renders")).Text);
    }
}
