// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class StandaloneAppTest
        : ServerTestBase<DevHostServerFixture<StandaloneApp.Program>>, IDisposable
    {
        private readonly ServerFixture _serverFixture;

        public StandaloneAppTest(BrowserFixture browserFixture, DevHostServerFixture<StandaloneApp.Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            _serverFixture = serverFixture;
            Navigate("/", noReload: true);
            WaitUntilLoaded();
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Blazor standalone", Browser.Title);
        }

        [Fact]
        public void HasHeading()
        {
            Assert.Equal("Hello, world!", Browser.FindElement(By.TagName("h1")).Text);
        }
        
        [Fact]
        public void ServesStaticAssetsFromClientAppWebRoot()
        {
            // Verify that bootstrap.js was loaded
            var javascriptExecutor = (IJavaScriptExecutor)Browser;
            var bootstrapTooltipType = javascriptExecutor
                .ExecuteScript("return typeof (window.Tooltip);");
            Assert.Equal("function", bootstrapTooltipType);
        }

        [Fact]
        public void NavMenuHighlightsCurrentLocation()
        {
            var activeNavLinksSelector = By.CssSelector(".main-nav a.active");
            var mainHeaderSelector = By.TagName("h1");

            // Verify we start at home, with the home link highlighted
            Assert.Equal("Hello, world!", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));

            // Click on the "counter" link
            Browser.FindElement(By.LinkText("Counter")).Click();

            // Verify we're now on the counter page, with that nav link (only) highlighted
            Assert.Equal("Counter", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Counter", item.Text));

            // Verify we can navigate back to home too
            Browser.FindElement(By.LinkText("Home")).Click();
            Assert.Equal("Hello, world!", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }

        public void Dispose()
        {
            // Make the tests run faster by navigating back to the home page when we are done
            // If we don't, then the next test will reload the whole page before it starts
            Browser.FindElement(By.LinkText("Home")).Click();
        }
    }
}
