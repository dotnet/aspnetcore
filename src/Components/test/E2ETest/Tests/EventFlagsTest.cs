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
}
