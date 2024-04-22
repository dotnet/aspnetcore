// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class EventBubblingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    // Note that currently we only support custom events if they have bubble:true.
    // That's because the event delegator doesn't know which custom events bubble and which don't,
    // so it doesn't know whether to register a normal handler or a capturing one. If this becomes
    // a problem, we could consider registering both types of handler and just bailing out from
    // the one that doesn't match the 'bubbles' flag on the received event object.

    public EventBubblingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<EventBubblingComponent>();
        Browser.Exists(By.Id("event-bubbling"));
    }

    [Fact]
    public void BubblingStandardEvent_FiredOnElementWithHandler()
    {
        Browser.Exists(By.Id("button-with-onclick")).Click();

        // Triggers event on target and ancestors with handler in upwards direction
        Browser.Equal(
            new[] { "target onclick", "parent onclick" },
            GetLogLines);
    }

    [Fact]
    public void BubblingStandardEvent_FiredOnElementWithoutHandler()
    {
        Browser.Exists(By.Id("button-without-onclick")).Click();

        // Triggers event on ancestors with handler in upwards direction
        Browser.Equal(
            new[] { "parent onclick" },
            GetLogLines);
    }

    [Fact]
    public void BubblingCustomEvent_FiredOnElementWithHandler()
    {
        TriggerCustomBubblingEvent("element-with-onsneeze", "sneeze");

        // Triggers event on target and ancestors with handler in upwards direction
        Browser.Equal(
            new[] { "target onsneeze", "parent onsneeze" },
            GetLogLines);
    }

    [Fact]
    public void BubblingCustomEvent_FiredOnElementWithoutHandler()
    {
        TriggerCustomBubblingEvent("element-without-onsneeze", "sneeze");

        // Triggers event on ancestors with handler in upwards direction
        Browser.Equal(
            new[] { "parent onsneeze" },
            GetLogLines);
    }

    [Fact]
    public void NonBubblingEvent_FiredOnElementWithHandler()
    {
        Browser.Exists(By.Id("input-with-onfocus")).Click();

        // Triggers event only on target, not other ancestors with event handler
        Browser.Equal(
            new[] { "target onfocus" },
            GetLogLines);
    }

    [Fact]
    public void NonBubblingEvent_FiredOnElementWithoutHandler()
    {
        Browser.Exists(By.Id("input-without-onfocus")).Click();

        // Triggers no event
        Browser.Empty(GetLogLines);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("intermediate")]
    public void StopPropagation(string whereToStopPropagation)
    {
        // If stopPropagation is off, we observe the event on the listener and all its ancestors
        Browser.Exists(By.Id("button-with-onclick")).Click();
        Browser.Equal(new[] { "target onclick", "parent onclick" }, GetLogLines);

        // If stopPropagation is on, the event doesn't reach the ancestor
        // Note that in the "intermediate element" case, the intermediate element does *not* itself
        // listen for this event, which shows that stopPropagation works independently of handling
        ClearLog();
        Browser.Exists(By.Id($"{whereToStopPropagation}-stop-propagation")).Click();
        Browser.Exists(By.Id("button-with-onclick")).Click();
        Browser.Equal(new[] { "target onclick" }, GetLogLines);

        // We can also toggle it back off
        ClearLog();
        Browser.Exists(By.Id($"{whereToStopPropagation}-stop-propagation")).Click();
        Browser.Exists(By.Id("button-with-onclick")).Click();
        Browser.Equal(new[] { "target onclick", "parent onclick" }, GetLogLines);
    }

    [Fact]
    public void PreventDefaultWorksOnTarget()
    {
        // Clicking a checkbox without preventDefault produces both "click" and "change"
        // events, and it becomes checked
        var checkboxWithoutPreventDefault = Browser.Exists(By.Id("checkbox-with-preventDefault-false"));
        checkboxWithoutPreventDefault.Click();
        Browser.Equal(new[] { "Checkbox click", "Checkbox change" }, GetLogLines);
        Browser.True(() => checkboxWithoutPreventDefault.Selected);

        // Clicking a checkbox with preventDefault produces a "click" event, but no "change"
        // event, and it remains unchecked
        ClearLog();
        var checkboxWithPreventDefault = Browser.Exists(By.Id("checkbox-with-preventDefault-true"));
        checkboxWithPreventDefault.Click();
        Browser.Equal(new[] { "Checkbox click" }, GetLogLines);
        Browser.False(() => checkboxWithPreventDefault.Selected);
    }

    [Fact]
    public void PreventDefault_WorksOnAncestorElement()
    {
        // Even though the checkbox we're clicking this case does *not* have preventDefault,
        // if its ancestor does, then we don't get the "change" event and it remains unchecked
        Browser.Exists(By.Id($"ancestor-prevent-default")).Click();
        var checkboxWithoutPreventDefault = Browser.Exists(By.Id("checkbox-with-preventDefault-false"));
        checkboxWithoutPreventDefault.Click();
        Browser.Equal(new[] { "Checkbox click" }, GetLogLines);
        Browser.False(() => checkboxWithoutPreventDefault.Selected);

        // We can also toggle it back off dynamically
        Browser.Exists(By.Id($"ancestor-prevent-default")).Click();
        ClearLog();
        checkboxWithoutPreventDefault.Click();
        Browser.Equal(new[] { "Checkbox click", "Checkbox change" }, GetLogLines);
        Browser.True(() => checkboxWithoutPreventDefault.Selected);
    }

    [Fact]
    public void PreventDefaultCanBlockKeystrokes()
    {
        // By default, the textbox accepts keystrokes
        var textbox = Browser.Exists(By.Id($"textbox-that-can-block-keystrokes"));
        textbox.SendKeys("a");
        Browser.Equal(new[] { "Received keydown" }, GetLogLines);
        Browser.Equal("a", () => textbox.GetAttribute("value"));

        // We can turn on preventDefault to stop keystrokes
        // There will still be a keydown event, but we're preventing it from actually changing the textbox value
        ClearLog();
        Browser.Exists(By.Id($"prevent-keydown")).Click();
        textbox.SendKeys("b");
        Browser.Equal(new[] { "Received keydown" }, GetLogLines);
        Browser.Equal("a", () => textbox.GetAttribute("value"));

        // We can turn it back off
        ClearLog();
        Browser.Exists(By.Id($"prevent-keydown")).Click();
        textbox.SendKeys("c");
        Browser.Equal(new[] { "Received keydown" }, GetLogLines);
        Browser.Equal("ac", () => textbox.GetAttribute("value"));
    }

    private string[] GetLogLines()
        => Browser.Exists(By.TagName("textarea"))
        .GetAttribute("value")
        .Replace("\r\n", "\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    void ClearLog()
        => Browser.Exists(By.Id("clear-log")).Click();

    private void TriggerCustomBubblingEvent(string elementId, string eventName)
    {
        var jsExecutor = (IJavaScriptExecutor)Browser;
        jsExecutor.ExecuteScript(
            $"window.testelem = document.getElementById('{elementId}')");
        jsExecutor.ExecuteScript(
            $"window.testelem.dispatchEvent(new Event('{eventName}', {{ bubbles: true }}))");
    }
}
