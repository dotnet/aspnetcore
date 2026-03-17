// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyRenderingTest : ServerTestBase<BlazorWasmTestAppFixture<BasicTestApp.Program>>
{
    public WebAssemblyRenderingTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<BasicTestApp.Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = "/subdir";
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
    }

    [Fact]
    public void CanUseAddMultipleAttributes_BoolAndStringValues()
    {
        var appElement = Browser.MountTestComponent<DuplicateAttributesComponent>();

        // Verify initial bool attribute (true) is rendered as presence-only attribute
        var selector = By.CssSelector("#duplicate-on-element > div");
        Browser.Exists(selector);
        var element = appElement.FindElement(selector);
        Assert.Equal(string.Empty, element.GetDomAttribute("bool"));
        Assert.Equal("middle-value", element.GetDomAttribute("string"));
        Assert.Equal("unmatched-value", element.GetDomAttribute("unmatched"));

        // Verify bool attribute overridden to false is absent
        selector = By.CssSelector("#duplicate-on-element-override > div");
        element = appElement.FindElement(selector);
        Assert.Null(element.GetDomAttribute("bool"));
        Assert.Equal("other-text", element.GetDomAttribute("string"));
        Assert.Equal("unmatched-value", element.GetDomAttribute("unmatched"));
    }

    [Fact]
    public void CanToggleBoolAttributeViaSplattedDictionary()
    {
        var appElement = Browser.MountTestComponent<DuplicateAttributesComponent>();

        var target = appElement.FindElement(By.Id("bool-toggle-target"));
        var toggleButton = appElement.FindElement(By.Id("bool-toggle-button"));
        var stateLabel = appElement.FindElement(By.Id("bool-toggle-state"));

        // Initial state: isBoolEnabled=true, so data-enabled is present, hidden is absent
        Browser.Equal("True", () => stateLabel.Text);
        Assert.Equal(string.Empty, target.GetDomAttribute("data-enabled"));
        Assert.Null(target.GetDomAttribute("hidden"));

        // Toggle to false: data-enabled removed, hidden added
        toggleButton.Click();
        Browser.Equal("False", () => stateLabel.Text);
        Assert.Null(target.GetDomAttribute("data-enabled"));
        Assert.Equal(string.Empty, target.GetDomAttribute("hidden"));

        // Toggle back to true: data-enabled present again, hidden removed
        toggleButton.Click();
        Browser.Equal("True", () => stateLabel.Text);
        Assert.Equal(string.Empty, target.GetDomAttribute("data-enabled"));
        Assert.Null(target.GetDomAttribute("hidden"));
    }

    [Fact]
    public void CanBindSelectWithBoolAttributes()
    {
        var appElement = Browser.MountTestComponent<DuplicateAttributesComponent>();

        var selectElement = new SelectElement(Browser.Exists(By.Id("select-with-bool-attrs")));
        var boundValue = appElement.FindElement(By.Id("select-with-bool-attrs-value"));
        var toggleDisabledButton = appElement.FindElement(By.Id("select-toggle-disabled"));
        var disabledState = appElement.FindElement(By.Id("select-disabled-state"));

        // Initial state: select is enabled, value is "Second"
        Assert.Equal("Second choice", selectElement.SelectedOption.Text);
        Assert.Equal("Second", boundValue.Text);
        Browser.Equal("False", () => disabledState.Text);
        Assert.Null(Browser.FindElement(By.Id("select-with-bool-attrs")).GetDomAttribute("disabled"));
        Assert.Equal("custom-value", Browser.FindElement(By.Id("select-with-bool-attrs")).GetDomAttribute("data-custom"));

        // Change selection while enabled
        selectElement.SelectByText("Third choice");
        Browser.Equal("Third", () => boundValue.Text);

        // Disable the select via bool attribute toggle
        toggleDisabledButton.Click();
        Browser.Equal("True", () => disabledState.Text);
        Assert.Equal(string.Empty, Browser.FindElement(By.Id("select-with-bool-attrs")).GetDomAttribute("disabled"));

        // Re-enable the select
        toggleDisabledButton.Click();
        Browser.Equal("False", () => disabledState.Text);
        Assert.Null(Browser.FindElement(By.Id("select-with-bool-attrs")).GetDomAttribute("disabled"));

        // Verify select still works after re-enabling
        selectElement.SelectByText("First choice");
        Browser.Equal("First", () => boundValue.Text);
    }

    [Fact]
    public void CanToggleMixedBoolAndStringAttributes()
    {
        var appElement = Browser.MountTestComponent<DuplicateAttributesComponent>();

        var input = appElement.FindElement(By.Id("mixed-attrs-input"));
        var toggleButton = appElement.FindElement(By.Id("mixed-attrs-toggle"));
        var stateLabel = appElement.FindElement(By.Id("mixed-attrs-state"));

        // Initial state: readonly=false (absent), data-active=true (present), placeholder is string
        Browser.Equal("False", () => stateLabel.Text);
        Assert.Null(input.GetDomAttribute("readonly"));
        Assert.Equal(string.Empty, input.GetDomAttribute("data-active"));
        Assert.Equal("type here", input.GetDomAttribute("placeholder"));

        // Toggle: readonly=true (present), data-active=false (absent), placeholder unchanged
        toggleButton.Click();
        Browser.Equal("True", () => stateLabel.Text);
        Assert.Equal(string.Empty, input.GetDomAttribute("readonly"));
        Assert.Null(input.GetDomAttribute("data-active"));
        Assert.Equal("type here", input.GetDomAttribute("placeholder"));

        // Toggle back: readonly removed, data-active restored
        toggleButton.Click();
        Browser.Equal("False", () => stateLabel.Text);
        Assert.Null(input.GetDomAttribute("readonly"));
        Assert.Equal(string.Empty, input.GetDomAttribute("data-active"));
        Assert.Equal("type here", input.GetDomAttribute("placeholder"));
    }
}
