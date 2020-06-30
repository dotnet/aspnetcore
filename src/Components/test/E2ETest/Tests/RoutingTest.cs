// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class RoutingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public RoutingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);
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
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
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
            SetUrlViaPushState($"/WithOptionalParameters");

            var app = Browser.MountTestComponent<TestRouter>();
            var expected = $"Your age is .";

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
        public void CanFollowLinkToOtherPageWithQueryString()
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with query")).Click();
            Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with query");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithQueryString()
        {
            SetUrlViaPushState("/Other");

            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with query")).Click();
            Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with query");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithHash()
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with hash")).Click();
            Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with hash");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithHash()
        {
            SetUrlViaPushState("/Other");

            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with hash")).Click();
            Browser.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with hash");
        }

        [Fact]
        public void CanFollowLinkToNotAComponent()
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Not a component")).Click();
            Browser.Equal("Not a component!", () => Browser.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanGoBackFromNotAComponent()
        {
            SetUrlViaPushState("/");

            // First go to some URL on the router
            var app = Browser.MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other")).Click();
            Browser.True(() => Browser.Url.EndsWith("/Other"));

            // Now follow a link out of the SPA entirely
            app.FindElement(By.LinkText("Not a component")).Click();
            Browser.Equal("Not a component!", () => Browser.FindElement(By.Id("test-info")).Text);
            Browser.True(() => Browser.Url.EndsWith("/NotAComponent.html"));

            // Now click back
            // Because of how the tests are structured with the router not appearing until the router
            // tests are selected, we can only observe the test selector being there, but this is enough
            // to show we did go back to the right place and the Blazor app started up
            Browser.Navigate().Back();
            Browser.True(() => Browser.Url.EndsWith("/Other"));
            Browser.WaitUntilTestSelectorReady();
        }

        [Fact]
        public void CanNavigateProgrammatically()
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            var testSelector = Browser.WaitUntilTestSelectorReady();

            app.FindElement(By.Id("do-navigation")).Click();
            Browser.True(() => Browser.Url.EndsWith("/Other"));
            Browser.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

            // Because this was client-side navigation, we didn't lose the state in the test selector
            Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetAttribute("value"));
        }

        [Fact]
        public void CanNavigateProgrammaticallyWithForceLoad()
        {
            SetUrlViaPushState("/");

            var app = Browser.MountTestComponent<TestRouter>();
            var testSelector = Browser.WaitUntilTestSelectorReady();

            app.FindElement(By.Id("do-navigation-forced")).Click();
            Browser.True(() => Browser.Url.EndsWith("/Other"));

            // Because this was a full-page load, our element references should no longer be valid
            Assert.Throws<StaleElementReferenceException>(() =>
            {
                testSelector.SelectedOption.GetAttribute("value");
            });
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

        [Theory]
        [InlineData("external", "ancestor")]
        [InlineData("external", "target")]
        [InlineData("external", "descendant")]
        [InlineData("internal", "ancestor")]
        [InlineData("internal", "target")]
        [InlineData("internal", "descendant")]
        public void PreventDefault_CanBlockNavigation(string navigationType, string whereToPreventDefault)
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

        private long BrowserScrollY
        {
            get => (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.scrollY");
            set => ((IJavaScriptExecutor)Browser).ExecuteScript($"window.scrollTo(0, {value})");
        }

        private string SetUrlViaPushState(string relativeUri)
        {
            var pathBaseWithoutHash = ServerPathBase.Split('#')[0];
            var jsExecutor = (IJavaScriptExecutor)Browser;
            var absoluteUri = new Uri(_serverFixture.RootUri, $"{pathBaseWithoutHash}{relativeUri}");
            jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.ToString().Replace("'", "\\'")}')");

            return absoluteUri.AbsoluteUri;
        }

        private void AssertHighlightedLinks(params string[] linkTexts)
        {
            Browser.Equal(linkTexts, () => Browser
                .FindElements(By.CssSelector("a.active"))
                .Select(x => x.Text));
        }
    }
}
