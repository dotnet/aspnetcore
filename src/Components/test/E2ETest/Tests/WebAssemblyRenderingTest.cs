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

        var toggleButton = appElement.FindElement(By.Id("bool-toggle-button"));
        var stateLabel = appElement.FindElement(By.Id("bool-toggle-state"));

        // Initial state: isBoolEnabled=true, so data-enabled is present, hidden is absent
        Browser.Equal("True", () => stateLabel.Text);
        Browser.True(() => BoolAttributeIsPresent("bool-toggle-target", "data-enabled"));
        Browser.True(() => !BoolAttributeIsPresent("bool-toggle-target", "hidden"));

        // Toggle to false: data-enabled removed, hidden added
        toggleButton.Click();
        Browser.Equal("False", () => stateLabel.Text);
        Browser.True(() => !BoolAttributeIsPresent("bool-toggle-target", "data-enabled"));
        Browser.True(() => BoolAttributeIsPresent("bool-toggle-target", "hidden"));

        // Toggle back to true: data-enabled present again, hidden removed
        toggleButton.Click();
        Browser.Equal("True", () => stateLabel.Text);
        Browser.True(() => BoolAttributeIsPresent("bool-toggle-target", "data-enabled"));
        Browser.True(() => !BoolAttributeIsPresent("bool-toggle-target", "hidden"));
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
        Browser.True(() => !BoolAttributeIsPresent("select-with-bool-attrs", "disabled"));
        Browser.Equal("custom-value", () => Browser.FindElement(By.Id("select-with-bool-attrs")).GetDomAttribute("data-custom"));

        // Change selection while enabled
        selectElement.SelectByText("Third choice");
        Browser.Equal("Third", () => boundValue.Text);

        // Disable the select via bool attribute toggle
        toggleDisabledButton.Click();
        Browser.Equal("True", () => disabledState.Text);
        Browser.True(() => BoolAttributeIsPresent("select-with-bool-attrs", "disabled"));

        // Re-enable the select
        toggleDisabledButton.Click();
        Browser.Equal("False", () => disabledState.Text);
        Browser.True(() => !BoolAttributeIsPresent("select-with-bool-attrs", "disabled"));

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
        Browser.True(() => !BoolAttributeIsPresent("mixed-attrs-input", "readonly"));
        Browser.True(() => BoolAttributeIsPresent("mixed-attrs-input", "data-active"));
        Browser.Equal("type here", () => input.GetDomAttribute("placeholder"));

        // Toggle: readonly=true (present), data-active=false (absent), placeholder unchanged
        toggleButton.Click();
        Browser.Equal("True", () => stateLabel.Text);
        Browser.True(() => BoolAttributeIsPresent("mixed-attrs-input", "readonly"));
        Browser.True(() => !BoolAttributeIsPresent("mixed-attrs-input", "data-active"));
        Browser.Equal("type here", () => input.GetDomAttribute("placeholder"));

        // Toggle back: readonly removed, data-active restored
        toggleButton.Click();
        Browser.Equal("False", () => stateLabel.Text);
        Browser.True(() => !BoolAttributeIsPresent("mixed-attrs-input", "readonly"));
        Browser.True(() => BoolAttributeIsPresent("mixed-attrs-input", "data-active"));
        Browser.Equal("type here", () => input.GetDomAttribute("placeholder"));
    }

    /// <summary>
    /// Checks attribute presence via JavaScript to avoid Selenium's GetDomAttribute
    /// returning "true" for standard HTML boolean attributes (hidden, disabled, readonly)
    /// instead of the actual attribute value.
    /// </summary>
    private bool BoolAttributeIsPresent(string elementId, string attributeName)
    {
        var js = (IJavaScriptExecutor)Browser;
        return (bool)js.ExecuteScript(
            "return document.getElementById(arguments[0]).hasAttribute(arguments[1])",
            elementId, attributeName);
    }
}
