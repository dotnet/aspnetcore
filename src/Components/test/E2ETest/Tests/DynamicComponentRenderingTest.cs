// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class DynamicComponentRenderingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    private IWebElement app;
    private SelectElement testCasePicker;

    public DynamicComponentRenderingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        app = Browser.MountTestComponent<DynamicComponentRendering>();
        testCasePicker = new SelectElement(app.FindElement(By.Id("dynamic-component-case-picker")));
    }

    [Fact]
    public void CanRenderComponentDynamically()
    {
        var hostRenderCountDisplay = app.FindElement(By.Id("outer-rendercount"));
        Browser.Equal("1", () => hostRenderCountDisplay.Text);

        testCasePicker.SelectByText("Counter");
        Browser.Equal("2", () => hostRenderCountDisplay.Text);

        // Basic rendering of a dynamic child works
        var childContainer = app.FindElement(By.Id("dynamic-child"));
        var currentCountDisplay = childContainer.FindElements(By.TagName("p")).First();
        Browser.Equal("Current count: 0", () => currentCountDisplay.Text);

        // The dynamic child can process events and re-render as normal
        var incrementButton = childContainer.FindElement(By.TagName("button"));
        incrementButton.Click();
        Browser.Equal("Current count: 1", () => currentCountDisplay.Text);

        // Re-rendering the child doesn't re-render the host
        Browser.Equal("2", () => hostRenderCountDisplay.Text);

        // Re-rendering the host doesn't lose state in the child (e.g., by recreating it)
        app.FindElement(By.Id("re-render-host")).Click();
        Browser.Equal("3", () => hostRenderCountDisplay.Text);
        Browser.Equal("Current count: 1", () => currentCountDisplay.Text);
        incrementButton.Click();
        Browser.Equal("Current count: 2", () => currentCountDisplay.Text);
    }

    [Fact]
    public void CanPassParameters()
    {
        testCasePicker.SelectByText("Component with parameters");
        var dynamicChild = app.FindElement(By.Id("dynamic-child"));

        // Regular parameters work
        Browser.Equal("Hello 123", () => dynamicChild.FindElement(By.CssSelector(".Param1 li")).Text);

        // Derived parameters work
        Browser.Equal("Goodbye Derived", () => dynamicChild.FindElement(By.CssSelector(".Param2")).Text);

        // Catch-all parameters work
        Browser.Equal("unmatchedParam This is the unmatched param value", () => dynamicChild.FindElement(By.CssSelector(".Param3 li")).Text);
    }

    [Fact]
    public void CanChangeDynamicallyRenderedComponent()
    {
        testCasePicker.SelectByText("Component with parameters");
        var dynamicChild = app.FindElement(By.Id("dynamic-child"));
        Browser.Equal("Component With Parameters", () => dynamicChild.FindElement(By.TagName("h3")).Text);

        testCasePicker.SelectByText("Counter");
        Browser.Equal("Counter", () => dynamicChild.FindElement(By.TagName("h1")).Text);
    }
}
