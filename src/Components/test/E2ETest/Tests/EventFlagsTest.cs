// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class EventFlagsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public EventFlagsTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<EventFlagsComponent>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnMouseDown_WithPreventDefaultEnabled_DoesNotFocusButton(bool handlersEnabled)
    {
        if (!handlersEnabled)
        {
            // Disable onmousedown handlers
            var toggleHandlers = Browser.Exists(By.Id("toggle-handlers"));
            toggleHandlers.Click();
        }

        var button = Browser.Exists(By.Id("mousedown-test-button"));
        button.Click();

        // Check that the button has not gained focus (should not be yellow)
        var afterClickBackgroundColor = button.GetCssValue("background-color");
        Assert.DoesNotContain("255, 255, 0", afterClickBackgroundColor);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnMouseDown_WithPreventDefaultDisabled_DoesFocusButton(bool handlersEnabled)
    {
        if (!handlersEnabled)
        {
            // Disable onmousedown handlers
            var toggleHandlers = Browser.Exists(By.Id("toggle-handlers"));
            toggleHandlers.Click();
        }

        // Disable preventDefault
        var togglePreventDefault = Browser.Exists(By.Id("toggle-prevent-default"));
        togglePreventDefault.Click();

        var button = Browser.Exists(By.Id("mousedown-test-button"));

        // Get the initial background color and check that it is no yellow
        var initialBackgroundColor = button.GetCssValue("background-color");
        Assert.DoesNotContain("255, 255, 0", initialBackgroundColor);

        button.Click();

        // Check that the button has gained focus (yellow background)
        var afterClickBackgroundColor = button.GetCssValue("background-color");
        Assert.Contains("255, 255, 0", afterClickBackgroundColor);
    }

    [Fact]
    public void OnClick_WithStopPropagationEnabled_DoesNotPropagateToParent()
    {
        var button = Browser.Exists(By.Id("stop-propagation-test-button"));
        button.Click();

        var eventLog = Browser.Exists(By.Id("event-log"));
        Assert.Contains("mousedown handler called on child", eventLog.Text);
        Assert.DoesNotContain("mousedown handler called on parent", eventLog.Text);
    }

    [Fact]
    public void OnClick_WithStopPropagationDisabled_PropagatesToParent()
    {
        // Disable stopPropagation
        var toggleStopPropagation = Browser.Exists(By.Id("toggle-stop-propagation"));
        toggleStopPropagation.Click();

        var button = Browser.Exists(By.Id("stop-propagation-test-button"));
        button.Click();

        var eventLog = Browser.Exists(By.Id("event-log"));
        Assert.Contains("mousedown handler called on child", eventLog.Text);
        Assert.Contains("mousedown handler called on parent", eventLog.Text);
    }

    [Fact]
    public void OnWheel_WithPreventDefaultEnabled_DoesNotScrollDiv()
    {
        var scrollableDiv = Browser.Exists(By.Id("wheel-test-area"));

        // Simulate a wheel scroll action
        var scrollOrigin = new WheelInputDevice.ScrollOrigin
        {
            Element = scrollableDiv,
        };
        new Actions(Browser)
            .ScrollFromOrigin(scrollOrigin, 0, 200)
            .Perform();

        // The Selenium scrolling action always changes the scrollTop property even when the event is prevented.
        // For this reason, we do not check for equality with zero.
        var newScrollTop = int.Parse(scrollableDiv.GetDomProperty("scrollTop"), CultureInfo.InvariantCulture);
        Assert.True(newScrollTop < 3);
    }

    [Fact]
    public void OnWheel_WithPreventDefaultDisabled_DoesScrollDiv()
    {
        // Disable preventDefault
        var togglePreventDefault = Browser.Exists(By.Id("toggle-prevent-default"));
        togglePreventDefault.Click();

        var scrollableDiv = Browser.Exists(By.Id("wheel-test-area"));

        // Simulate a wheel scroll action
        var scrollOrigin = new WheelInputDevice.ScrollOrigin
        {
            Element = scrollableDiv,
        };
        new Actions(Browser)
            .ScrollFromOrigin(scrollOrigin, 0, 200)
            .Perform();

        // The Selenium scrolling action is not precise and changes the scrollTop property to e.g. 202 instead of 200.
        // For this reason, we do not check for equality with specific value.
        var newScrollTop = int.Parse(scrollableDiv.GetDomProperty("scrollTop"), CultureInfo.InvariantCulture);
        Assert.True(newScrollTop >= 200);
    }

    [Fact]
    public void OnInput_WithDebounce_CoalescesRapidKeystrokes()
    {
        var input = Browser.Exists(By.Id("debounced-input"));
        input.SendKeys("hello");

        Browser.Equal("hello", () => Browser.Exists(By.Id("debounced-value")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("debounced-count")).Text);
    }

    [Fact]
    public void OnClick_WithThrottle_DropsClicksDuringInterval()
    {
        var button = Browser.Exists(By.Id("throttled-click-button"));

        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "const btn = arguments[0]; btn.click(); btn.click(); btn.click();",
            button);

        Browser.Equal("1", () => Browser.Exists(By.Id("throttled-click-count")).Text);
    }

    [Fact]
    public void OnScroll_WithThrottle_DeliversLatestScrollPosition()
    {
        var scrollArea = Browser.Exists(By.Id("throttled-scroll-area"));

        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "const el = arguments[0];" +
            "el.scrollTop = 50; el.dispatchEvent(new Event('scroll'));" +
            "el.scrollTop = 100; el.dispatchEvent(new Event('scroll'));" +
            "el.scrollTop = 150; el.dispatchEvent(new Event('scroll'));",
            scrollArea);

        // Leading edge + trailing edge = 2 events total for 3 rapid scroll events
        Browser.Equal("2", () => Browser.Exists(By.Id("throttled-scroll-count")).Text);
    }

    [Fact]
    public void OnInput_WithDebounceAndKeyHandlers_DoesNotFlushOnTypingKeys_AndFlushesOnEnter()
    {
        var input = Browser.Exists(By.Id("debounced-combo-input"));
        input.SendKeys("abc");

        Browser.Equal("abc", () => Browser.Exists(By.Id("combo-value")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("combo-input-count")).Text);
        Browser.Equal("3", () => Browser.Exists(By.Id("combo-keydown-count")).Text);
        Browser.Equal("3", () => Browser.Exists(By.Id("combo-keyup-count")).Text);

        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "const el = arguments[0];" +
            "el.value = 'abc-enter';" +
            "el.dispatchEvent(new Event('input', { bubbles: true }));" +
            "el.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));",
            input);

        Browser.Equal("abc-enter", () => Browser.Exists(By.Id("combo-enter-committed")).Text);
        Browser.Equal("2", () => Browser.Exists(By.Id("combo-input-count")).Text);
    }

    [Fact]
    public void OnInput_WithDebounce_HoverDoesNotDeliverPendingInput_ButClickingSuggestionDoes()
    {
        var input = Browser.Exists(By.Id("typeahead-input"));
        var suggestion = Browser.Exists(By.Id("typeahead-suggestion"));
        input.SendKeys("app");

        new Actions(Browser).MoveToElement(suggestion).Perform();
        Browser.Equal("[]", () => Browser.Exists(By.Id("typeahead-query-on-hover")).Text);

        // Pressing the suggestion blurs the input, which delivers the typed text before the click
        suggestion.Click();
        Browser.Equal("[app]", () => Browser.Exists(By.Id("typeahead-query-on-click")).Text);
        Assert.Equal("[]", Browser.Exists(By.Id("typeahead-query-on-hover")).Text);
    }

    [Fact]
    public void OnInput_WithDebounce_ClickOutsideFocusedInputDeliversPendingInputWithoutBlur()
    {
        var input = Browser.Exists(By.Id("typeahead-input"));
        input.SendKeys("app");

        // The suggestion cancels mousedown, so the input keeps focus and no blur is raised.
        // Assistive technologies click elements that can't take focus the same way.
        Browser.Exists(By.Id("typeahead-suggestion-keeping-focus")).Click();
        Browser.Equal("[app]", () => Browser.Exists(By.Id("typeahead-query-on-focus-keeping-click")).Text);
        Assert.Equal("typeahead-input", Browser.SwitchTo().ActiveElement().GetDomProperty("id"));
    }

    [Fact]
    public void OnChange_WithThrottle_DeliversLatestSelection()
    {
        var select = Browser.Exists(By.Id("throttled-select"));

        // The first change is dispatched at once, and the latest one when the interval ends
        select.SendKeys(Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown);

        Browser.Equal("4", () => Browser.Exists(By.Id("throttled-select-value")).Text);
        Assert.Equal("4", select.GetDomProperty("value"));
    }

    [Fact]
    public void OnInput_WithDebounce_RemovingFocusedInputDropsPendingInput()
    {
        var input = Browser.Exists(By.Id("removable-input"));
        input.SendKeys("abc");

        // Chrome blurs the input while the render removes it, which must not deliver the pending text to the removed handler
        input.SendKeys(Keys.Escape);
        Browser.DoesNotExist(By.Id("removable-input"));

        // Showing the input again goes through .NET, so anything dispatched during the removal has been processed by then
        Browser.Exists(By.Id("show-removable-input")).Click();
        Browser.Exists(By.Id("removable-input"));
        Assert.Equal(string.Empty, Browser.Exists(By.Id("removable-value")).Text);
        Assert.Equal("none", Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
    }
}
