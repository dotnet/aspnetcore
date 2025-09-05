// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class RoutingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public RoutingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.RoutingTestContext);

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.WaitUntilTestSelectorReady();
    }

    [Fact]
    public void CanArriveAtDefaultPage()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
    }

    [Fact]
    public void CanArriveAtDefaultPageWithoutTrailingSlash()
    {
        // This is a bit of a degenerate case because ideally devs would configure their
        // servers to enforce a canonical URL (with trailing slash) for the homepage.
        // But in case they don't want to, we need to handle it the same as if the URL does
        // have a trailing slash.
        SetUrlViaPushState("");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)", "Default, no trailing slash (matches all)");
    }

    [Fact]
    public void CanArriveAtPageWithParameters()
    {
        SetUrlViaPushState("/WithParameters/Name/Ghi/LastName/O'Jkl");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Your full name is Ghi O'Jkl.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks();
    }

    [Fact]
    public void CanArriveAtPageWithNumberParameters()
    {
        var testInt = int.MinValue;
        var testLong = long.MinValue;
        var testDec = -2.33333m;
        var testDouble = -1.489d;
        var testFloat = -2.666f;

        SetUrlViaPushState($"/WithNumberParameters/{testInt}/{testLong}/{testDouble}/{testFloat}/{testDec}");

        var app = Browser.MountTestComponent<TestRouter>();
        var expected = $"Test parameters: {testInt} {testLong} {testDouble} {testFloat} {testDec}";

        Assert.Equal(expected, app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanArriveAtPageWithOptionalParametersProvided()
    {
        var testAge = 101;

        SetUrlViaPushState($"/WithOptionalParameters/{testAge}");

        var app = Browser.MountTestComponent<TestRouter>();
        var expected = $"Your age is {testAge}.";

        Assert.Equal(expected, app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanArriveAtPageWithOptionalParametersNotProvided()
    {
        SetUrlViaPushState($"/WithOptionalParameters?query=ignored");

        var app = Browser.MountTestComponent<TestRouter>();
        var expected = $"Your age is .";

        Assert.Equal(expected, app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanArriveAtPageWithCatchAllParameter()
    {
        SetUrlViaPushState("/WithCatchAllParameter/life/the/universe/and/everything%20%3D%2042?query=ignored");

        var app = Browser.MountTestComponent<TestRouter>();
        var expected = $"The answer: life/the/universe/and/everything = 42.";

        Assert.Equal(expected, app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanArriveAtNonDefaultPage()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
    }

    [Fact]
    public void CanArriveAtFallbackPageFromBadURI()
    {
        SetUrlViaPushState("/Oopsie_Daisies%20%This_Aint_A_Real_Page");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Oops, that component wasn't found!", app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanFollowLinkToOtherPage()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Other")).Click();
        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
    }

    [Fact]
    public void CanFollowLinkToOtherPageWithCtrlClick()
    {
        // On macOS we need to hold the command key not the control for opening a popup
        var key = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Keys.Command : Keys.Control;

        try
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            var button = app.FindElement(By.LinkText("Other"));

            new Actions(Browser).KeyDown(key).Click(button).Build().Perform();

            Browser.Equal(2, () => Browser.WindowHandles.Count);
        }
        finally
        {
            // Leaving the ctrl key up
            new Actions(Browser).KeyUp(key).Build().Perform();

            // Closing newly opened windows if a new one was opened
            while (Browser.WindowHandles.Count > 1)
            {
                Browser.SwitchTo().Window(Browser.WindowHandles.Last());
                Browser.Close();
            }

            // Needed otherwise Selenium tries to direct subsequent commands
            // to the tab that has already been closed
            Browser.SwitchTo().Window(Browser.WindowHandles.First());
        }
    }

    [Fact]
    public void CanFollowLinkToTargetBlankClick()
    {
        try
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();

            app.FindElement(By.LinkText("Target (_blank)")).Click();

            Browser.Equal(2, () => Browser.WindowHandles.Count);
        }
        finally
        {
            // Closing newly opened windows if a new one was opened
            while (Browser.WindowHandles.Count > 1)
            {
                Browser.SwitchTo().Window(Browser.WindowHandles.Last());
                Browser.Close();
            }

            // Needed otherwise Selenium tries to direct subsequent commands
            // to the tab that has already been closed
            Browser.SwitchTo().Window(Browser.WindowHandles.First());
        }
    }

    [Fact]
    public void CanFollowLinkToOtherPageDoesNotOpenNewWindow()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();

        app.FindElement(By.LinkText("Other")).Click();

        Assert.Single(Browser.WindowHandles);
    }

    [Fact]
    public void CanFollowLinkToOtherPageWithBaseRelativeUrl()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Other with base-relative URL (matches all)")).Click();
        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
    }

    [Fact]
    public void CanFollowLinkToEmptyStringHrefAsBaseRelativeUrl()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default with base-relative URL (matches all)")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
    }

    [Fact]
    public void CanFollowLinkToPageWithParameters()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("With parameters")).Click();
        Browser.Equal("Your full name is Abc .", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("With parameters");

        // Can add more parameters while remaining on same page
        app.FindElement(By.LinkText("With more parameters")).Click();
        Browser.Equal("Your full name is Abc McDef.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("With parameters", "With more parameters");

        // Can remove parameters while remaining on same page
        app.FindElement(By.LinkText("With parameters")).Click();
        Browser.Equal("Your full name is Abc .", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("With parameters");
    }

    [Fact]
    public void CanFollowLinkToDefaultPage()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default (matches all)")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
    }

    [Fact]
    public void CanFollowLinkToDefaultPage_NoTrailingSlash()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default, no trailing slash (matches all)")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)", "Default, no trailing slash (matches all)");
    }

    [Fact]
    public void CanFollowLinkToOtherPageWithQueryString()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Other with query")).Click();
        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)", "Other with query");
    }

    [Fact]
    public void CanFollowLinkToDefaultPageWithQueryString()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default with query")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks(
            "Default (matches all)",
            "Default with base-relative URL (matches all)",
            "Default with query");
    }

    [Fact]
    public void CanFollowLinkToDefaultPageWithQueryString_NoTrailingSlash()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default with query, no trailing slash")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks(
            "Default (matches all)",
            "Default with base-relative URL (matches all)",
            "Default, no trailing slash (matches all)",
            "Default with query, no trailing slash");
    }

    [Fact]
    public void CanFollowLinkToOtherPageWithHash()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Other with hash")).Click();
        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)", "Other with hash");
    }

    [Fact]
    public void CanFollowLinkToDefaultPageWithHash()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default with hash")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks(
            "Default (matches all)",
            "Default with base-relative URL (matches all)",
            "Default with hash");
    }

    [Fact]
    public void CanFollowLinkToDefaultPageWithHash_NoTrailingSlash()
    {
        SetUrlViaPushState("/Other");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Default with hash, no trailing slash")).Click();
        Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks(
            "Default (matches all)",
            "Default with base-relative URL (matches all)",
            "Default, no trailing slash (matches all)",
            "Default with hash, no trailing slash");
    }

    [Fact]
    public void CanFollowLinkToNotAComponent()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Not a component")).Click();
        Browser.Equal("Not a component!", () => Browser.Exists(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanFollowLinkDefinedInOpenShadowRoot()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();

        // It's difficult to access elements within a shadow root using Selenium's regular APIs
        // Bypass this limitation by clicking the element via JavaScript
        var shadowHost = app.FindElement(By.TagName("custom-link-with-shadow-root"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("arguments[0].shadowRoot.querySelector('a').click()", shadowHost);

        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
    }

    [Fact]
    public void CanOverrideNavLinkToNotIgnoreFragment()
    {
        SetUrlViaPushState("/layout-overridden/for-hash");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Override layout with hash, no trailing slash")).Click();
        Browser.Equal("This is the page with overridden layout.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Override layout with hash, no trailing slash");
    }

    [Fact]
    public void CanOverrideNavLinkToNotIgnoreQuery()
    {
        SetUrlViaPushState("/layout-overridden");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Override layout with query, no trailing slash")).Click();
        Browser.Equal("This is the page with overridden layout.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Override layout with query, no trailing slash");
    }

    [Fact]
    public void CanGoBackFromNotAComponent()
    {
        SetUrlViaPushState("/");

        // First go to some URL on the router
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Other")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));

        // Now follow a link out of the SPA entirely
        app.FindElement(By.LinkText("Not a component")).Click();
        Browser.Equal("Not a component!", () => Browser.Exists(By.Id("test-info")).Text);
        Browser.True(() => Browser.Url.EndsWith("/NotAComponent.html", StringComparison.Ordinal));

        // Now click back
        // Because of how the tests are structured with the router not appearing until the router
        // tests are selected, we can only observe the test selector being there, but this is enough
        // to show we did go back to the right place and the Blazor app started up
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        Browser.WaitUntilTestSelectorReady();
    }

    [Fact]
    public void CanNavigateProgrammatically()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.Id("do-navigation")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        // Because this was client-side navigation, we didn't lose the state in the test selector
        Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetDomProperty("value"));
    }

    [Fact]
    public void CanNavigateProgrammaticallyWithForceLoad()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.Id("do-navigation-forced")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));

        // Because this was a full-page load, our element references should no longer be valid
        Assert.Throws<StaleElementReferenceException>(() =>
        {
            testSelector.SelectedOption.GetDomProperty("value");
        });
    }

    [Fact]
    public void CanNavigateProgrammaticallyWithStateValidateNoReplaceHistoryEntry()
    {
        // This test checks if default navigation does not replace Browser history entries
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        // We navigate to the /Other page
        app.FindElement(By.Id("do-other-navigation-state")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        Browser.Contains("state", () => app.FindElement(By.Id("test-state")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        // After we press back, we should end up at the "/ProgrammaticNavigationCases" page so we know browser history has not been replaced
        // If history had been replaced we would have ended up at the "/" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        AssertHighlightedLinks("Programmatic navigation cases");

        // When the navigation is forced, the state is ignored (we could choose to throw here).
        app.FindElement(By.Id("do-other-navigation-forced-state")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        Browser.DoesNotExist(By.Id("test-state"));

        // We check if we had a force load
        Assert.Throws<StaleElementReferenceException>(() =>
            testSelector.SelectedOption.GetDomProperty("value"));

        // But still we should be able to navigate back, and end up at the "/ProgrammaticNavigationCases" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.WaitUntilTestSelectorReady();
    }

    [Fact]
    public void CanNavigateProgrammaticallyWithStateReplaceHistoryEntry()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        // We navigate to the /Other page, with "replace" enabled
        app.FindElement(By.Id("do-other-navigation-state-replacehistoryentry")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        Browser.Contains("state", () => app.FindElement(By.Id("test-state")).Text);
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        // After we press back, we should end up at the "/" page so we know browser history has been replaced
        // If history would not have been replaced we would have ended up at the "/ProgrammaticNavigationCases" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/", StringComparison.Ordinal));
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");

        // Because this was all with client-side navigation, we didn't lose the state in the test selector
        Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetDomProperty("value"));
    }

    [Fact]
    public void NavigationToSamePathDoesNotScrollToTheTop()
    {
        // This test checks if the navigation to same path or path with query appeneded,
        // keeps the scroll in the position from before navigation
        // but moves it when we navigate to a fragment
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        var jsExecutor = (IJavaScriptExecutor)Browser;
        var maxScrollPosition = (long)jsExecutor.ExecuteScript("return document.documentElement.scrollHeight - window.innerHeight;");
        // scroll max up to find the position of fragment
        BrowserScrollY = 0;
        var fragmentScrollPosition = (long)jsExecutor.ExecuteScript("return document.getElementById('fragment').getBoundingClientRect().top + window.scrollY;");

        // scroll maximally down
        BrowserScrollY = maxScrollPosition;

        app.FindElement(By.Id("do-self-navigate")).Click();
        WaitAssert.True(Browser, () => maxScrollPosition == BrowserScrollY, default, $"Expected to stay scrolled down in {maxScrollPosition} but the scroll is in position {BrowserScrollY}.");

        app.FindElement(By.Id("do-self-navigate-with-query")).Click();
        WaitAssert.True(Browser, () => maxScrollPosition == BrowserScrollY, default, $"Expected to stay scrolled down in {maxScrollPosition} but the scroll is in position {BrowserScrollY}.");

        app.FindElement(By.Id("do-self-navigate-to-fragment")).Click();
        WaitAssert.True(Browser, () => fragmentScrollPosition == BrowserScrollY, default, $"Expected to scroll to the fragment in position {fragmentScrollPosition} but the scroll is in position {BrowserScrollY}.");
    }

    [Fact]
    public void CanNavigateProgrammaticallyValidateNoReplaceHistoryEntry()
    {
        // This test checks if default navigation does not replace Browser history entries
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        // We navigate to the /Other page
        // This will also test our new NavigateTo(string uri) overload (it should not replace the browser history)
        app.FindElement(By.Id("do-other-navigation")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        // After we press back, we should end up at the "/ProgrammaticNavigationCases" page so we know browser history has not been replaced
        // If history had been replaced we would have ended up at the "/" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        AssertHighlightedLinks("Programmatic navigation cases");

        // For completeness, we will test if the normal NavigateTo(string uri, bool forceLoad) overload will also
        // NOT change the browser's history. So we basically repeat what we have done above.
        app.FindElement(By.Id("do-other-navigation2")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        AssertHighlightedLinks("Programmatic navigation cases");

        // Because this was client-side navigation, we didn't lose the state in the test selector
        Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetDomProperty("value"));

        app.FindElement(By.Id("do-other-navigation-forced")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));

        // We check if we had a force load
        Assert.Throws<StaleElementReferenceException>(() =>
            testSelector.SelectedOption.GetDomProperty("value"));

        // But still we should be able to navigate back, and end up at the "/ProgrammaticNavigationCases" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.WaitUntilTestSelectorReady();
    }

    [Fact]
    public void CanNavigateProgrammaticallyWithReplaceHistoryEntry()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        // We navigate to the /Other page, with "replace" enabled
        app.FindElement(By.Id("do-other-navigation-replacehistoryentry")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));
        AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

        // After we press back, we should end up at the "/" page so we know browser history has been replaced
        // If history would not have been replaced we would have ended up at the "/ProgrammaticNavigationCases" page
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/", StringComparison.Ordinal));
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");

        // Because this was all with client-side navigation, we didn't lose the state in the test selector
        Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetDomProperty("value"));
    }

    [Fact]
    public void CanNavigateProgrammaticallyWithForceLoadAndReplaceHistoryEntry()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        var testSelector = Browser.WaitUntilTestSelectorReady();

        app.FindElement(By.LinkText("Programmatic navigation cases")).Click();
        Browser.True(() => Browser.Url.EndsWith("/ProgrammaticNavigationCases", StringComparison.Ordinal));
        Browser.Contains("programmatic navigation", () => app.FindElement(By.Id("test-info")).Text);

        // We navigate to the /Other page, with replacehistroyentry and forceload enabled
        app.FindElement(By.Id("do-other-navigation-forced-replacehistoryentry")).Click();
        Browser.True(() => Browser.Url.EndsWith("/Other", StringComparison.Ordinal));

        // We check if we had a force load
        Assert.Throws<StaleElementReferenceException>(() =>
            testSelector.SelectedOption.GetDomProperty("value"));

        // After we press back, we should end up at the "/" page so we know browser history has been replaced
        Browser.Navigate().Back();
        Browser.True(() => Browser.Url.EndsWith("/", StringComparison.Ordinal));
        Browser.WaitUntilTestSelectorReady();
    }

    [Fact]
    public void ClickingAnchorWithNoHrefShouldNotNavigate()
    {
        SetUrlViaPushState("/");
        var initialUrl = Browser.Url;

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.Id("anchor-with-no-href")).Click();

        Assert.Equal(initialUrl, Browser.Url);
        AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
    }

    [Theory]
    [InlineData("/Other-With-Hyphens", "Other with hyphens")]
    [InlineData("/Other.With.Dots", "Other with dots")]
    [InlineData("/Other_With_Underscores", "Other with underscores")]
    [InlineData("/Other~With~Tildes", "Other with tildes")]
    public void RoutePrefixDoesNotMatchWithNonSeparatorCharacters(string url, string linkText)
    {
        SetUrlViaPushState(url);

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks(linkText); // The 'Other' link text should not be highlighted.
    }

    [Fact]
    public void UsingNavigationManagerWithoutRouterWorks()
    {
        var app = Browser.MountTestComponent<NavigationManagerComponent>();
        var initialUrl = Browser.Url;

        Browser.Equal(Browser.Url, () => app.FindElement(By.Id("test-info")).Text);
        var uri = SetUrlViaPushState("/mytestpath");
        Browser.Equal(uri, () => app.FindElement(By.Id("test-info")).Text);

        var jsExecutor = (IJavaScriptExecutor)Browser;
        jsExecutor.ExecuteScript("history.back()");

        Browser.Equal(initialUrl, () => app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void UriHelperCanReadAbsoluteUriIncludingHash()
    {
        var app = Browser.MountTestComponent<NavigationManagerComponent>();
        Browser.Equal(Browser.Url, () => app.FindElement(By.Id("test-info")).Text);

        var uri = "/mytestpath?my=query&another#some/hash?tokens";
        var expectedAbsoluteUri = $"{_serverFixture.RootUri}subdir{uri}";

        SetUrlViaPushState(uri);
        Browser.Equal(expectedAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void NavigationLock_CanBlockNavigation_ThenContinue()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add two navigation locks that block internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-1 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var relativeUriPostNavigation = "/mytestpath";
        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir{relativeUriPostNavigation}";

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation was blocked and the navigation controls are displaying
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));
        Browser.Exists(By.CssSelector("#navigation-lock-1 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // Unblock the first navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // Wait until the navigation controls have disappeared before continuing
        Browser.DoesNotExist(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The second navigation lock is still blocking navigation
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Unblock the second navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-1 > div.blocking-controls > button.navigation-continue")).Click();

        // The navigation finally continues
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_CanBlockNavigation_ThenCancel()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add two navigation locks that block internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-1 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;

        SetUrlViaPushState("/mytestpath");

        // Both navigation locks have initiated their "location changing" handlers and are displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));
        Browser.Exists(By.CssSelector("#navigation-lock-1 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // Cancel the navigation using the first navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-cancel")).Click();

        // The second navigation lock callback has completed and has thus removed its navigation controls
        Browser.DoesNotExist(By.CssSelector("#navigation-lock-1 > div.blocking-controls"));

        // The navigation was canceled and the URI has not changed
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was still not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_CanAddAndRemoveLocationChangingCallback()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var relativeUriPostNavigation = "/mytestpath";
        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir{relativeUriPostNavigation}";

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Cancel the navigation using the first navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-cancel")).Click();

        // The navigation lock callback has completed and so the navigation controls have been removed
        Browser.DoesNotExist(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // The navigation was canceled and the URI has not changed
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Remove the location changing callback
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation was not blocked because the location changed callback parameter was removed
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_RemovesLock_WhenDisposed()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var relativeUriPostNavigation = "/mytestpath";
        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir{relativeUriPostNavigation}";

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The navigation was blocked
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // Cancel the navigation using the first navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-cancel")).Click();

        // The navigation lock callback has completed and so the navigation controls have been removed
        Browser.DoesNotExist(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The navigation was canceled and the URI has not changed
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Remove the navigation lock component
        Browser.FindElement(By.Id("remove-navigation-lock")).Click();

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation was not blocked because the lock was removed when the navigation lock component was disposed
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_OverlappingNavigationsCancelExistingNavigations_PushState()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var relativeCanceledUri = "/mycanceledtestpath";
        var expectedCanceledAbsoluteUri = $"{_serverFixture.RootUri}subdir{relativeCanceledUri}";

        SetUrlViaPushState(relativeCanceledUri);

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        var relativeUriPostNavigation = "/mytestpath";
        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir{relativeUriPostNavigation}";

        SetUrlViaPushState(relativeUriPostNavigation);

        // The navigation was canceled and logged
        Browser.Equal($"Canceling '{expectedCanceledAbsoluteUri}'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > p.navigation-log > span.navigation-log-entry-0"))?.Text);

        // The location was reverted again
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Unblock the new navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // We navigated to the updated URL
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_OverlappingNavigationsCancelExistingNavigations_HistoryNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        SetUrlViaPushState("/mytestpath0");
        SetUrlViaPushState("/mytestpath1");

        // The "LocationChanged" event was called twice
        Browser.Equal("2", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // We have the expected initial URI
        var expectedStartingAbsoluteUri = $"{_serverFixture.RootUri}subdir/mytestpath1";
        Browser.Equal(expectedStartingAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        Browser.Navigate().Back();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Equal(expectedStartingAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(expectedStartingAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called after the two initial navigations
        Browser.Equal("2", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        Browser.Navigate().Back();

        // The first navigation was canceled and logged
        var expectedCanceledAbsoluteUri = $"{_serverFixture.RootUri}subdir/mytestpath0";
        Browser.Equal($"Canceling '{expectedCanceledAbsoluteUri}'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > p.navigation-log > span.navigation-log-entry-0"))?.Text);

        // Unblock the new navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // We navigated to the updated URL
        var expectedPostNavigationAbsoluteUri = $"{_serverFixture.RootUri}subdir/mytestpath0";
        Browser.Equal(expectedPostNavigationAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("3", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_OverlappingNavigationsCancelExistingNavigations_ProgrammaticNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var expectedCanceledRelativeUri = $"/subdir/some-path-0";

        Browser.FindElement(By.Id("programmatic-navigation")).Click();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir/some-path-1";

        Browser.FindElement(By.Id("programmatic-navigation")).Click();

        // The navigation was canceled and logged
        Browser.Equal($"Canceling '{expectedCanceledRelativeUri}'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > p.navigation-log > span.navigation-log-entry-0"))?.Text);

        // The location was reverted again
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Unblock the new navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // We navigated to the updated URL
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_OverlappingNavigationsCancelExistingNavigations_InternalLinkNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;

        var expectedCanceledAbsoluteUri = $"{_serverFixture.RootUri}subdir/some-path-0";

        Browser.FindElement(By.Id("internal-link-navigation")).Click();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        var expectedAbsoluteUriPostNavigation = $"{_serverFixture.RootUri}subdir/some-path-1";

        Browser.FindElement(By.Id("increment-link-navigation-index")).Click();
        Browser.FindElement(By.Id("internal-link-navigation")).Click();

        // The navigation was canceled and logged
        Browser.Equal($"Canceling '{expectedCanceledAbsoluteUri}'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > p.navigation-log > span.navigation-log-entry-0"))?.Text);

        // The location was reverted again
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Unblock the new navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // We navigated to the updated URL
        Browser.Equal(expectedAbsoluteUriPostNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact]
    public void NavigationLock_HistoryNavigationWorks_AfterRefresh()
    {
        SetUrlViaPushState("/");
        SetUrlViaPushState("/mytestpath0");
        SetUrlViaPushState("/mytestpath1");

        Browser.Navigate().Refresh();

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        // We have the expected initial URI
        var expectedStartingAbsoluteUri = $"{_serverFixture.RootUri}subdir/mytestpath1";
        Browser.Equal(expectedStartingAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        Browser.Navigate().Back();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(expectedStartingAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        // The "LocationChanged" event was not called
        Browser.Equal("0", () => app.FindElement(By.Id("location-changed-count"))?.Text);

        // Unblock the navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // The navigation was continued
        var expectedFinalAbsoluteUri = $"{_serverFixture.RootUri}subdir/mytestpath0";
        Browser.Equal(expectedFinalAbsoluteUri, () => app.FindElement(By.Id("test-info")).Text);

        // The navigation was logged
        Browser.Equal($"Continuing '{expectedFinalAbsoluteUri}'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > p.navigation-log > span.navigation-log-entry-0"))?.Text);

        // The "LocationChanged" event was called
        Browser.Equal("1", () => app.FindElement(By.Id("location-changed-count"))?.Text);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/57153")]
    public void NavigationLock_CanBlockExternalNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add two navigation locks that block external navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.confirm-external-navigation")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-1 > input.confirm-external-navigation")).Click();

        SetAbsluteUrlViaPushState($"{_serverFixture.RootUri}/myexternalpath");

        // Dismiss the confirmation prompt that pops up
        Browser.SwitchTo().Alert().Dismiss();

        // The navigation was canceled and we're on the sarting URI
        var expectedStartingUri = $"{_serverFixture.RootUri}subdir/";
        Browser.Equal(expectedStartingUri, () => app.FindElement(By.Id("test-info")).Text);

        // Disable external navigation confirmation on one of the navigation locks
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.confirm-external-navigation")).Click();

        SetAbsluteUrlViaPushState($"{_serverFixture.RootUri}/myexternalpath2");

        // Dismiss the confirmation prompt that pops up
        Browser.SwitchTo().Alert().Dismiss();

        // The navigation was canceled again and we're on the sarting URI
        Browser.Equal(expectedStartingUri, () => app.FindElement(By.Id("test-info")).Text);

        // Disable external navigation confirmation on the other navigation lock
        Browser.FindElement(By.CssSelector("#navigation-lock-1 > input.confirm-external-navigation")).Click();

        var expectedFinalUri = $"{_serverFixture.RootUri}/myexternalpath3";
        SetAbsluteUrlViaPushState(expectedFinalUri);

        // The external navigation was not blocked
        Browser.Equal(expectedFinalUri, () => Browser.Url);
    }

    [Fact]
    public void NavigationLock_CanReadHistoryStateEntry_InLocationChangingHandler()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();

        // Add a history entry
        Browser.FindElement(By.Id("programmatic-navigation")).Click();

        //var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;
        var expectedInitialUri = $"{_serverFixture.RootUri}subdir/some-path-0";
        Browser.Equal(expectedInitialUri, () => app.FindElement(By.Id("test-info")).Text);

        // Block internal navigations
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        // Add another history entry
        Browser.FindElement(By.Id("programmatic-navigation")).Click();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The state was captured in the programmatically-initiated navigation.
        Browser.Equal("State = 'Navigation index 1'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > span.history-state"))?.Text);

        // Unblock the navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // The location reflects what it should be after the navigation completes
        var expectedFinalUri = $"{_serverFixture.RootUri}subdir/some-path-1";
        Browser.Equal(expectedFinalUri, () => app.FindElement(By.Id("test-info")).Text);

        Browser.Navigate().Back();

        // The state was captured in the browser-initiated navigation.
        Browser.Equal("State = 'Navigation index 0'", () => app.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > span.history-state"))?.Text);

        // Unblock the navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-continue")).Click();

        // The location reflects what it should be after the navigation completes
        Browser.Equal(expectedInitialUri, () => app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void NavigationLock_CanRenderUIForExceptions_ProgrammaticNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;

        Browser.FindElement(By.Id("programmatic-navigation")).Click();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Throw an exception for the current navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-throw-exception")).Click();

        // The exception shows up in the UI
        var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
        Assert.NotNull(errorUiElem);
    }

    [Fact]
    public void NavigationLock_CanRenderUIForExceptions_InternalLinkNavigation()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<NavigationManagerComponent>();

        // Add a navigation lock that blocks internal navigations
        Browser.FindElement(By.Id("add-navigation-lock")).Click();
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > input.block-internal-navigation")).Click();

        var uriBeforeBlockedNavigation = Browser.FindElement(By.Id("test-info")).Text;

        Browser.FindElement(By.Id("internal-link-navigation")).Click();

        // The navigation lock has initiated its "location changing" handler and is displaying navigation controls
        Browser.Exists(By.CssSelector("#navigation-lock-0 > div.blocking-controls"));

        // The location was reverted to what it was before the navigation started
        Browser.Equal(uriBeforeBlockedNavigation, () => app.FindElement(By.Id("test-info")).Text);

        // Throw an exception for the current navigation
        Browser.FindElement(By.CssSelector("#navigation-lock-0 > div.blocking-controls > button.navigation-throw-exception")).Click();

        // The exception shows up in the UI
        var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
        Assert.NotNull(errorUiElem);
    }

    [Fact]
    public void CanArriveAtRouteWithExtension()
    {
        // This is an odd test, but it's primarily here to verify routing for routeablecomponentfrompackage isn't available due to
        // some unknown reason
        SetUrlViaPushState("/Default.html");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        AssertHighlightedLinks("With extension");
    }

    [Fact]
    public void RoutingToComponentOutsideMainAppDoesNotWork()
    {
        SetUrlViaPushState("/routeablecomponentfrompackage.html");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Oops, that component wasn't found!", app.FindElement(By.Id("test-info")).Text);
    }

    [Fact]
    public void RoutingToComponentOutsideMainAppWorksWithAdditionalAssemblySpecified()
    {
        SetUrlViaPushState("/routeablecomponentfrompackage.html");

        var app = Browser.MountTestComponent<TestRouterWithAdditionalAssembly>();
        Assert.Contains("This component, including the CSS and image required to produce its", app.FindElement(By.CssSelector("div.special-style")).Text);
    }

    [Fact]
    public void ResetsScrollPositionWhenPerformingInternalNavigation_LinkClick()
    {
        SetUrlViaPushState("/LongPage1");
        var app = Browser.MountTestComponent<TestRouter>();
        Browser.Equal("This is a long page you can scroll.", () => app.FindElement(By.Id("test-info")).Text);
        BrowserScrollY = 500;
        Browser.True(() => BrowserScrollY > 300); // Exact position doesn't matter

        app.FindElement(By.LinkText("Long page 2")).Click();
        Browser.Equal("This is another long page you can scroll.", () => app.FindElement(By.Id("test-info")).Text);
        Browser.Equal(0, () => BrowserScrollY);
    }

    [Fact]
    public void ResetsScrollPositionWhenPerformingInternalNavigation_ProgrammaticNavigation()
    {
        SetUrlViaPushState("/LongPage1");
        var app = Browser.MountTestComponent<TestRouter>();
        Browser.Equal("This is a long page you can scroll.", () => app.FindElement(By.Id("test-info")).Text);
        BrowserScrollY = 500;
        Browser.True(() => BrowserScrollY > 300); // Exact position doesn't matter

        app.FindElement(By.Id("go-to-longpage2")).Click();
        Browser.Equal("This is another long page you can scroll.", () => app.FindElement(By.Id("test-info")).Text);
        Browser.Equal(0, () => BrowserScrollY);
    }

    [Fact]
    public void Refresh_FullyReloadsTheCurrentPage()
    {
        SetUrlViaPushState("/");

        Browser.MountTestComponent<NavigationManagerComponent>();
        Browser.FindElement(By.Id("programmatic-refresh")).Click();

        // If the page fully reloads, the NavigationManagerComponent will no longer be mounted
        Browser.DoesNotExist(By.Id("programmatic-refresh"));
    }

    [Fact]
    public void PreventDefault_CanBlockNavigation_ForInternalNavigation_PreventDefaultTarget()
        => PreventDefault_CanBlockNavigation("internal", "target");

    [Fact]
    public void PreventDefault_CanBlockNavigation_ForExternalNavigation_PreventDefaultAncestor()
        => PreventDefault_CanBlockNavigation("external", "ancestor");

    [Theory]
    [InlineData("external", "target")]
    [InlineData("external", "descendant")]
    [InlineData("internal", "ancestor")]
    [InlineData("internal", "descendant")]
    public virtual void PreventDefault_CanBlockNavigation(string navigationType, string whereToPreventDefault)
    {
        SetUrlViaPushState("/PreventDefaultCases");
        var app = Browser.MountTestComponent<TestRouter>();
        var preventDefaultToggle = app.FindElement(By.CssSelector($".prevent-default .{whereToPreventDefault}"));
        var linkElement = app.FindElement(By.Id($"{navigationType}-navigation"));
        var counterButton = app.FindElement(By.ClassName("counter-button"));
        if (whereToPreventDefault == "descendant")
        {
            // We're testing clicks on the link's descendant element
            linkElement = linkElement.FindElement(By.TagName("span"));
        }

        // If preventDefault is on, then navigation does not occur
        preventDefaultToggle.Click();
        linkElement.Click();

        // We check that no navigation ocurred by observing that we can still use the counter
        counterButton.Click();
        Browser.Equal("Counter: 1", () => counterButton.Text);

        // Now if we toggle preventDefault back off, then navigation will occur
        preventDefaultToggle.Click();
        linkElement.Click();

        if (navigationType == "external")
        {
            Browser.Equal("about:blank", () => Browser.Url);
        }
        else
        {
            Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }
    }

    [Fact]
    public void OnNavigate_CanRenderLoadingFragment()
    {
        var app = Browser.MountTestComponent<TestRouterWithOnNavigate>();

        SetUrlViaPushState("/LongPage1");

        Browser.Exists(By.Id("loading-banner"));
    }

    [Fact]
    public void OnNavigate_CanCancelCallback()
    {
        var app = Browser.MountTestComponent<TestRouterWithOnNavigate>();

        // Navigating from one page to another should
        // cancel the previous OnNavigate Task
        SetUrlViaPushState("/LongPage2");
        SetUrlViaPushState("/LongPage1");

        AssertDidNotLog("I'm not happening...");
    }

    [Fact]
    public void OnNavigate_CanRenderUIForExceptions()
    {
        var app = Browser.MountTestComponent<TestRouterWithOnNavigate>();

        SetUrlViaPushState("/Other");

        var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
        Assert.NotNull(errorUiElem);
    }

    [Fact]
    public void OnNavigate_CanRenderUIForSyncExceptions()
    {
        var app = Browser.MountTestComponent<TestRouterWithOnNavigate>();

        // Should capture exception from synchronously thrown
        SetUrlViaPushState("/WithLazyAssembly");

        var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
        Assert.NotNull(errorUiElem);
    }

    [Fact]
    public void OnNavigate_DoesNotRenderWhileOnNavigateExecuting()
    {
        var app = Browser.MountTestComponent<TestRouterWithOnNavigate>();

        // Navigate to a route
        SetUrlViaPushState("/WithParameters/name/Abc");

        // Click the button to trigger a re-render
        var button = app.FindElement(By.Id("trigger-rerender"));
        button.Click();

        // Assert that the parameter route didn't render
        Browser.DoesNotExist(By.Id("test-info"));

        // Navigate to another page to cancel the previous `OnNavigateAsync`
        // task and trigger a re-render on its completion
        SetUrlViaPushState("/LongPage1");

        // Confirm that the route was rendered
        Browser.Equal("This is a long page you can scroll.", () => app.FindElement(By.Id("test-info")).Text);
    }

    [Theory]
    [InlineData("/WithParameters/Name/Ñoño ñi/LastName/O'Jkl")]
    [InlineData("/WithParameters/Name/[Ñoño ñi]/LastName/O'Jkl")]
    [InlineData("/other?abc=Ñoño ñi")]
    [InlineData("/other?abc=[Ñoño ñi]")]
    public void CanArriveAtPageWithSpecialURL(string relativeUrl)
    {
        SetUrlViaPushState(relativeUrl, true);
        var errorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Equal("none", () => errorUi.GetCssValue("display"));
    }

    [Fact]
    public void FocusOnNavigation_SetsFocusToMatchingElement()
    {
        // Applies focus on initial load
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        Browser.True(() => GetFocusedElement().Text == "This is the default page.");

        // Updates focus after navigation to regular page
        app.FindElement(By.LinkText("Other")).Click();
        Browser.True(() => GetFocusedElement().Text == "This is another page.");

        // If there's no matching element, we leave the focus unchanged
        app.FindElement(By.Id("with-lazy-assembly")).Click();
        Browser.Exists(By.Id("use-package-button"));
        Browser.Equal("a", () => GetFocusedElement().TagName);

        // No errors from lack of matching element - app still functions
        app.FindElement(By.LinkText("Other")).Click();
        Browser.True(() => GetFocusedElement().Text == "This is another page.");

        IWebElement GetFocusedElement()
            => Browser.SwitchTo().ActiveElement();
    }

    [Fact]
    public void CanArriveAtQueryStringPageWithNoQuery()
    {
        SetUrlViaPushState("/WithQueryParameters/Abc");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Hello Abc .", app.FindElement(By.Id("test-info")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-QueryInt")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);

        AssertHighlightedLinks("With query parameters (none)");
    }

    [Fact]
    public void CanArriveAtQueryStringPageWithStringQuery()
    {
        SetUrlViaPushState("/WithQueryParameters/Abc?stringvalue=Hello+there#123");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Hello Abc .", app.FindElement(By.Id("test-info")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-QueryInt")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal("Hello there", app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);

        AssertHighlightedLinks("With query parameters (none)", "With query parameters (passing string value)");
    }

    [Fact]
    public void CanArriveAtQueryStringPageWithDateTimeQuery()
    {
        var dateTime = new DateTime(2000, 1, 2, 3, 4, 5, 6);
        var dateOnly = new DateOnly(2000, 1, 2);
        var timeOnly = new TimeOnly(3, 4, 5, 6);
        SetUrlViaPushState($"/WithQueryParameters/Abc?NullableDateTimeValue=2000-01-02%2003:04:05&NullableDateOnlyValue=2000-01-02&NullableTimeOnlyValue=03:04:05");

        var app = Browser.MountTestComponent<TestRouter>();
        Assert.Equal("Hello Abc .", app.FindElement(By.Id("test-info")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-QueryInt")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(dateTime.ToString("hh:mm:ss on yyyy-MM-dd", CultureInfo.InvariantCulture), app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(timeOnly.ToString("hh:mm:ss", CultureInfo.InvariantCulture), app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);

        AssertHighlightedLinks("With query parameters (none)", "With query parameters (passing Date Time values)");
    }

    [Fact]
    public void CanNavigateToQueryStringPageWithNoQuery()
    {
        SetUrlViaPushState("/");

        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("With query parameters (none)")).Click();

        Assert.Equal("Hello Abc .", app.FindElement(By.Id("test-info")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-QueryInt")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);

        AssertHighlightedLinks("With query parameters (none)");
    }

    [Fact]
    public void CanNavigateWithinPageWithQueryStrings()
    {
        SetUrlViaPushState("/");

        // Navigate to a page with querystring
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("With query parameters (passing string value)")).Click();

        Browser.Equal("Hello Abc .", () => app.FindElement(By.Id("test-info")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-QueryInt")).Text);
        Assert.Equal("0", app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal("Hello there", app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);
        var instanceId = app.FindElement(By.Id("instance-id")).Text;
        Assert.True(!string.IsNullOrWhiteSpace(instanceId));

        AssertHighlightedLinks("With query parameters (none)", "With query parameters (passing string value)");

        // We can also navigate to a different query while retaining the same component instance
        app.FindElement(By.LinkText("With IntValue and LongValues")).Click();
        Browser.Equal("123", () => app.FindElement(By.Id("value-QueryInt")).Text);
        Browser.Equal("123", () => app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("3 values (50, 100, -20)", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("3 values (50, 100, -20)", app.FindElement(By.Id("value-nested-LongValues")).Text);
        Assert.Equal(instanceId, app.FindElement(By.Id("instance-id")).Text);
        AssertHighlightedLinks("With query parameters (none)");

        // We can also click back to go the preceding query while retaining the same component instance
        Browser.Navigate().Back();
        Browser.Equal("0", () => app.FindElement(By.Id("value-QueryInt")).Text);
        Browser.Equal("0", () => app.FindElement(By.Id("value-nested-QueryInt")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateTimeValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableDateOnlyValue")).Text);
        Assert.Equal(string.Empty, app.FindElement(By.Id("value-NullableTimeOnlyValue")).Text);
        Assert.Equal("Hello there", app.FindElement(By.Id("value-StringValue")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-LongValues")).Text);
        Assert.Equal("0 values ()", app.FindElement(By.Id("value-nested-LongValues")).Text);
        Assert.Equal(instanceId, app.FindElement(By.Id("instance-id")).Text);
        AssertHighlightedLinks("With query parameters (none)", "With query parameters (passing string value)");
    }

    [Fact]
    public void CanNavigateBetweenDifferentPagesWithQueryStrings()
    {
        SetUrlViaPushState("/");

        // Navigate between pages with the same querystring parameter "l" for LongValues and GuidValue.
        // https://github.com/dotnet/aspnetcore/issues/52483
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("With query parameters (none)")).Click();
        app.FindElement(By.LinkText("With IntValue and LongValues")).Click();
        app.FindElement(By.LinkText("Another page with GuidValue")).Click();

        Browser.Equal("8b7ae9ee-de22-4dd0-8fa1-b31e66abcc79", () => app.FindElement(By.Id("value-QueryGuid")).Text);
        // Verify that OnParametersSet was only called once.
        Browser.Equal("1", () => app.FindElement(By.Id("param-set-count")).Text);

        app.FindElement(By.LinkText("Another page with LongValues")).Click();
        Assert.Equal("3 values (50, 100, -20)", app.FindElement(By.Id("value-LongValues")).Text);

        // We can also click back to go the preceding query while retaining the same component instance.
        Browser.Navigate().Back();
        Browser.Equal("8b7ae9ee-de22-4dd0-8fa1-b31e66abcc79", () => app.FindElement(By.Id("value-QueryGuid")).Text);
    }

    [Fact]
    public void AnchorWithHrefContainingHashSamePage_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("anchor-test1")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void AnchorWithHrefToSameUrlWithQueryAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("anchor-test1-with-query")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash?color=green&number=123#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void AnchorWithHrefToSameUrlWithParamAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("anchor-test1-with-param")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash/11#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void AnchorWithHrefToSameUrlWithParamQueryAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("anchor-test1-with-param-and-query")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash/11?color=green&number=123#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void AnchorWithHrefContainingHashAnotherPage_NavigatesToPageAndScrollsToElement()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("anchor-test2")).Click();

        var test2VerticalLocation = app.FindElement(By.Id("test2")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash2#test2";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test2VerticalLocation, default, $"Expected {test2VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void NavigationManagerNavigateToAnotherUrlWithHash_NavigatesToPageAndScrollsToElement()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("navigate-test2")).Click();

        var test2VerticalLocation = app.FindElement(By.Id("test2")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash2#test2";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test2VerticalLocation, default, $"Expected {test2VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void NavigationManagerNavigateToSameUrlWithHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("navigate-test1")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void NavigationManagerNavigateToSameUrlWithQueryAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("navigate-test1-with-query")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash?color=green&number=123#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void NavigationManagerNavigateToSameUrlWithParamAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("navigate-test1-with-param")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash/22#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    [Fact]
    public void NavigationManagerNavigateToSameUrlWithParamQueryAndHash_ScrollsToElementOnTheSamePage()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Long page with hash")).Click();

        app.FindElement(By.Id("navigate-test1-with-param-and-query")).Click();

        var test1VerticalLocation = app.FindElement(By.Id("test1")).Location.Y;
        var currentRelativeUrl = _serverFixture.RootUri.MakeRelativeUri(new Uri(Browser.Url)).ToString();
        string expectedUrl = "subdir/LongPageWithHash/22?color=green&number=123#test1";
        WaitAssert.True(Browser, () => expectedUrl == currentRelativeUrl, default, $"Expected {expectedUrl} but got {currentRelativeUrl}");
        WaitAssert.True(Browser, () => BrowserScrollY == test1VerticalLocation, default, $"Expected {test1VerticalLocation} but got {BrowserScrollY}");
    }

    private long BrowserScrollY
    {
        get => Convert.ToInt64(((IJavaScriptExecutor)Browser).ExecuteScript("return window.scrollY"), CultureInfo.CurrentCulture);
        set => ((IJavaScriptExecutor)Browser).ExecuteScript($"window.scrollTo(0, {value})");
    }

    private string SetUrlViaPushState(string relativeUri, bool forceLoad = false)
    {
        var pathBaseWithoutHash = ServerPathBase.Split('#')[0];
        var absoluteUri = new Uri(_serverFixture.RootUri, $"{pathBaseWithoutHash}{relativeUri}");
        SetAbsluteUrlViaPushState(absoluteUri.ToString(), forceLoad);

        return absoluteUri.AbsoluteUri;
    }

    private void SetAbsluteUrlViaPushState(string absoluteUri, bool forceLoad = false)
    {
        var jsExecutor = (IJavaScriptExecutor)Browser;
        jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.Replace("'", "\\'")}', {(forceLoad ? "true" : "false")})");
    }

    private void AssertDidNotLog(params string[] messages)
    {
        var log = Browser.Manage().Logs.GetLog(LogType.Browser);
        foreach (var message in messages)
        {
            Assert.DoesNotContain(log, entry => entry.Message.Contains(message));
        }
    }

    private void AssertHighlightedLinks(params string[] linkTexts)
    {
        Browser.Equal(linkTexts, () => Browser
            .FindElements(By.CssSelector("a.active"))
            .Select(x => x.Text));
    }

    [Fact]
    public void ClickOnAnchorInsideSVGElementGetsIntercepted()
    {
        SetUrlViaPushState("/");
        var app = Browser.MountTestComponent<TestRouter>();
        app.FindElement(By.LinkText("Anchor inside SVG Element")).Click();

        Browser.Equal("0", () => Browser.Exists(By.Id("location-changed-count")).Text);

        Browser.FindElement(By.Id("svg-link")).Click();

        // If the click was intercepted then LocationChanged works
        Browser.Equal("1", () => Browser.Exists(By.Id("location-changed-count")).Text);
    }
}
