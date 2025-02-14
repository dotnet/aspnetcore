// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class EventCustomArgsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public EventCustomArgsTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // Always do a full page reload because these tests need to start with no custom event registrations
        Navigate(ServerPathBase);
        Browser.MountTestComponent<EventCustomArgsComponent>();
    }

    [Fact]
    public void UnregisteredCustomEventWorks()
    {
        // This reflects functionality in 5.0 and earlier, in which you could have custom events
        // registered with the Razor compiler but no way to register them with the runtime, so
        // you could only receive empty eventargs.
        Browser.Exists(By.Id("trigger-testevent-directly")).Click();
        Browser.Equal("Received testevent with args '{ MyProp=null }'", () => GetLogLines().Single());
    }

    [Fact]
    public void CanRegisterCustomEventAfterRender_WithNoCreateEventArgs()
    {
        Browser.Exists(By.Id("register-testevent-with-no-createventargs")).Click();
        Browser.FindElement(By.Id("trigger-testevent-directly")).Click();
        Browser.Equal("Received testevent with args '{ MyProp=null }'", () => GetLogLines().Single());
    }

    [Fact]
    public void CanRegisterCustomEventAfterRender_WithCreateEventArgsReturningNull()
    {
        Browser.Exists(By.Id("register-testevent-with-createventargs-that-returns-null")).Click();
        Browser.FindElement(By.Id("trigger-testevent-directly")).Click();
        Browser.Equal("Received testevent with args 'null'", () => GetLogLines().Single());
    }

    [Fact]
    public void CanRegisterCustomEventAfterRender_WithCreateEventArgsReturningData()
    {
        Browser.Exists(By.Id("register-testevent-with-createventargs-that-supplies-args")).Click();
        Browser.FindElement(By.Id("trigger-testevent-directly")).Click();
        Browser.Equal("Received testevent with args '{ MyProp=Native event target ID=test-event-target-child }'", () => GetLogLines().Single());
    }

    [Fact]
    public void CanAliasBrowserEvent_WithCreateEventArgsReturningData()
    {
        var input = Browser.Exists(By.CssSelector("#test-event-target-child input"));
        Browser.FindElement(By.Id("register-custom-keydown")).Click();
        SendKeysSequentially(input, "ab");

        Browser.Equal(new[]
        {
                "Received native keydown event",
                "You pressed: a",
                "Received native keydown event",
                "You pressed: b",
            }, GetLogLines);

        Assert.Equal("ab", input.GetDomProperty("value"));
    }

    [Fact]
    public void CanAliasBrowserEvent_PreventDefaultOnNativeEvent()
    {
        var input = Browser.Exists(By.CssSelector("#test-event-target-child input"));
        Browser.FindElement(By.Id("register-custom-keydown")).Click();
        Browser.FindElement(By.Id("custom-keydown-prevent-default")).Click();
        SendKeysSequentially(input, "ab");

        Browser.Equal(new[]
        {
                "Received native keydown event",
                "You pressed: a",
                "Received native keydown event",
                "You pressed: b",
            }, GetLogLines);

        // Check it was actually preventDefault-ed
        Assert.Equal("", input.GetDomProperty("value"));
    }

    [Fact]
    public void CanAliasBrowserEvent_StopPropagationIndependentOfNativeEvent()
    {
        var input = Browser.Exists(By.CssSelector("#test-event-target-child input"));
        Browser.FindElement(By.Id("register-custom-keydown")).Click();
        Browser.FindElement(By.Id("register-yet-another-keydown")).Click();
        Browser.FindElement(By.Id("custom-keydown-stop-propagation")).Click();
        SendKeysSequentially(input, "ab");

        Browser.Equal(new[]
        {
                // The native event still bubbles up to its listener on an ancestor, and
                // other aliased events still receive it, but the stopPropagation-ed
                // variant does not
                "Received native keydown event",
                "Yet another aliased event received: a",
                "Received native keydown event",
                "Yet another aliased event received: b",
            }, GetLogLines);

        Assert.Equal("ab", input.GetDomProperty("value"));
    }

    [Fact]
    public void CanHaveMultipleAliasesForASingleBrowserEvent()
    {
        var input = Browser.Exists(By.CssSelector("#test-event-target-child input"));
        Browser.FindElement(By.Id("register-custom-keydown")).Click();
        Browser.FindElement(By.Id("register-yet-another-keydown")).Click();
        SendKeysSequentially(input, "ab");

        Browser.Equal(new[]
        {
                "Received native keydown event",
                "You pressed: a",
                "Yet another aliased event received: a",
                "Received native keydown event",
                "You pressed: b",
                "Yet another aliased event received: b",
            }, GetLogLines);

        Assert.Equal("ab", input.GetDomProperty("value"));
    }

    [Fact]
    public void CanAliasBrowserEvent_WithoutAnyNativeListenerForBrowserEvent()
    {
        // Sets up a registration for a custom event name that's an alias for mouseover,
        // but there's no regular listener for mouseover in the application at this point
        Browser.Exists(By.Id("register-custom-mouseover")).Click();

        new Actions(Browser)
            .MoveToElement(Browser.FindElement(By.Id("test-event-target-child")))
            .Perform();

        // Nonetheless, the custom event is still received
        Browser.True(() => GetLogLines().Contains("Received custom mouseover event"));
    }

    [Fact]
    public void CanRegisterCustomEventAndSupplyComplexParams()
    {
        Browser.Exists(By.Id("register-sendjsobject")).Click();
        Browser.FindElement(By.Id("trigger-sendjsobject-event-directly")).Click();
        Browser.Collection(() => GetLogLines(),
            line => Assert.Equal("Received DotNetObject with property: This is correct", line),
            line => Assert.Equal("Received byte array of length 7 and first entry 1", line),
            line => Assert.Equal("Event with IJSObjectReference received: Hello!", line));
    }

    void SendKeysSequentially(IWebElement target, string text)
    {
        foreach (var c in text)
        {
            target.SendKeys(c.ToString());
        }
    }

    private string[] GetLogLines()
        => Browser.Exists(By.Id("test-log"))
        .GetDomProperty("value")
        .Replace("\r\n", "\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);
}
