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
    public class RoutingTest : BasicTestAppTestBase
    {
        public RoutingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: false);
            WaitUntilTestSelectorReady();
        }

        [Fact]
        public void CanArriveAtDefaultPage()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
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

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanArriveAtPageWithParameters()
        {
            SetUrlViaPushState("/WithParameters/Name/Ghi/LastName/O'Jkl");

            var app = MountTestComponent<TestRouter>();
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

            var app = MountTestComponent<TestRouter>();
            var expected = $"Test parameters: {testInt} {testLong} {testDouble} {testFloat} {testDec}";

            Assert.Equal(expected, app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanArriveAtNonDefaultPage()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }

        [Fact]
        public void CanArriveAtFallbackPageFromBadURI()
        {
            SetUrlViaPushState("/Oopsie_Daisies%20%This_Aint_A_Real_Page"); 

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("Oops, that component wasn't found!", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToOtherPage()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other")).Click();
            WaitAssert.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
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

                var app = MountTestComponent<TestRouter>();
                var button = app.FindElement(By.LinkText("Other"));
              
                new Actions(Browser).KeyDown(key).Click(button).Build().Perform();

                WaitAssert.Equal(2, () => Browser.WindowHandles.Count);
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

                var app = MountTestComponent<TestRouter>(); 

                app.FindElement(By.LinkText("Target (_blank)")).Click();

                WaitAssert.Equal(2, () => Browser.WindowHandles.Count);
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

            var app = MountTestComponent<TestRouter>();
            
            app.FindElement(By.LinkText("Other")).Click();
            
            Assert.Single(Browser.WindowHandles);
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithBaseRelativeUrl()
        {
            SetUrlViaPushState("/");            

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with base-relative URL (matches all)")).Click();
            WaitAssert.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToEmptyStringHrefAsBaseRelativeUrl()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with base-relative URL (matches all)")).Click();
            WaitAssert.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToPageWithParameters()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("With parameters")).Click();
            WaitAssert.Equal("Your full name is Abc .", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("With parameters");

            // Can add more parameters while remaining on same page
            app.FindElement(By.LinkText("With more parameters")).Click();
            WaitAssert.Equal("Your full name is Abc McDef.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("With parameters", "With more parameters");

            // Can remove parameters while remaining on same page
            // WARNING: This only works because the WithParameters component overrides SetParametersAsync
            // and explicitly resets its parameters to default when each new set of parameters arrives.
            // Without that, the page would retain the old value.
            // See https://github.com/aspnet/AspNetCore/issues/6864 where we reverted the logic to auto-reset.
            app.FindElement(By.LinkText("With parameters")).Click();
            WaitAssert.Equal("Your full name is Abc .", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("With parameters");
        }

        [Fact]
        public void CanFollowLinkToDefaultPage()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default (matches all)")).Click();
            WaitAssert.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithQueryString()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with query")).Click();
            WaitAssert.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with query");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithQueryString()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with query")).Click();
            WaitAssert.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with query");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithHash()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with hash")).Click();
            WaitAssert.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with hash");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithHash()
        {
            SetUrlViaPushState("/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with hash")).Click();
            WaitAssert.Equal("This is the default page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with hash");
        }

        [Fact]
        public void CanNavigateProgrammatically()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
            var testSelector = WaitUntilTestSelectorReady();

            app.FindElement(By.Id("do-navigation")).Click();
            WaitAssert.True(() => Browser.Url.EndsWith("/Other"));
            WaitAssert.Equal("This is another page.", () => app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");

            // Because this was client-side navigation, we didn't lose the state in the test selector
            Assert.Equal(typeof(TestRouter).FullName, testSelector.SelectedOption.GetAttribute("value"));
        }

        [Fact]
        public void CanNavigateProgrammaticallyWithForceLoad()
        {
            SetUrlViaPushState("/");

            var app = MountTestComponent<TestRouter>();
            var testSelector = WaitUntilTestSelectorReady();

            app.FindElement(By.Id("do-navigation-forced")).Click();
            WaitAssert.True(() => Browser.Url.EndsWith("/Other"));

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

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.Id("anchor-with-no-href")).Click();

            Assert.Equal(initialUrl, Browser.Url);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        private void SetUrlViaPushState(string relativeUri)
        {
            var pathBaseWithoutHash = ServerPathBase.Split('#')[0];
            var jsExecutor = (IJavaScriptExecutor)Browser;
            var absoluteUri = new Uri(_serverFixture.RootUri, $"{pathBaseWithoutHash}{relativeUri}");
            jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.ToString().Replace("'", "\\'")}')");
        }

        private void AssertHighlightedLinks(params string[] linkTexts)
        {
            WaitAssert.Equal(linkTexts, () => Browser
                .FindElements(By.CssSelector("a.active"))
                .Select(x => x.Text));
        }
    }
}
