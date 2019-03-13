// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerSideAppTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public ServerSideAppTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            _serverFixture.Environment = AspNetEnvironment.Development;
            _serverFixture.BuildWebHostMethod = ComponentsApp.Server.Program.BuildWebHost;

            Navigate("/", noReload: false);
            WaitUntilLoaded();
        }


        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Razor Components", Browser.Title);
        }

        [Fact]
        public void HasHeading()
        {
            Assert.Equal("Hello, world!", Browser.FindElement(By.TagName("h1")).Text);
        }

        [Fact]
        public void NavMenuHighlightsCurrentLocation()
        {
            var activeNavLinksSelector = By.CssSelector(".sidebar a.active");
            var mainHeaderSelector = By.TagName("h1");

            // Verify we start at home, with the home link highlighted
            Assert.Equal("Hello, world!", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));

            // Click on the "counter" link
            Browser.FindElement(By.LinkText("Counter")).Click();

            // Verify we're now on the counter page, with that nav link (only) highlighted
            WaitAssert.Equal("Counter", () => Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Counter", item.Text));

            // Verify we can navigate back to home too
            Browser.FindElement(By.LinkText("Home")).Click();
            WaitAssert.Equal("Hello, world!", () => Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));
        }

        [Fact]
        public void HasCounterPage()
        {
            // Navigate to "Counter"
            Browser.FindElement(By.LinkText("Counter")).Click();
            WaitAssert.Equal("Counter", () => Browser.FindElement(By.TagName("h1")).Text);

            // Observe the initial value is zero
            var countDisplayElement = Browser.FindElement(By.CssSelector("h1 + p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Click the button; see it counts
            var button = Browser.FindElement(By.CssSelector(".main button"));
            button.Click();
            WaitAssert.Equal("Current count: 1", () => countDisplayElement.Text);
            button.Click();
            WaitAssert.Equal("Current count: 2", () => countDisplayElement.Text);
            button.Click();
            WaitAssert.Equal("Current count: 3", () => countDisplayElement.Text);
        }

        [Fact]
        public void HasFetchDataPage()
        {
            // Navigate to "Fetch Data"
            Browser.FindElement(By.LinkText("Fetch data")).Click();
            WaitAssert.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

            // Wait until loaded
            var tableSelector = By.CssSelector("table.table");
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                driver => driver.FindElement(tableSelector) != null);

            // Check the table is displayed correctly
            var rows = Browser.FindElements(By.CssSelector("table.table tbody tr"));
            Assert.Equal(5, rows.Count);
            var cells = rows.SelectMany(row => row.FindElements(By.TagName("td")));
            foreach (var cell in cells)
            {
                Assert.True(!string.IsNullOrEmpty(cell.Text));
            }
        }

        [Fact]
        public void ReconnectUI()
        {
            Browser.FindElement(By.LinkText("Counter")).Click();
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript(@"
window.modalDisplayState = [];
window.Blazor.circuitHandlers.push({
    onConnectionUp: () => window.modalDisplayState.push(document.getElementById('components-reconnect-modal').style.display),
    onConnectionDown: () => window.modalDisplayState.push(document.getElementById('components-reconnect-modal').style.display)
});
window.Blazor._internal.forceCloseConnection();");

            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                driver => (long)javascript.ExecuteScript("console.log(window.modalDisplayState); return window.modalDisplayState.length") == 2);

            var states = (string)javascript.ExecuteScript("return window.modalDisplayState.join(',')");

            Assert.Equal("block,none", states);
        }

        [Fact]
        public void RendersContinueAfterReconnect()
        {
            Browser.FindElement(By.LinkText("Ticker")).Click();
            var selector = By.ClassName("tick-value");
            var element = Browser.FindElement(selector);

            var initialValue = element.Text;

            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript(@"
window.connectionUp = false;
window.Blazor.circuitHandlers.push({
    onConnectionUp: () => window.connectionUp = true
});
window.Blazor._internal.forceCloseConnection();");

            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                driver => (bool)javascript.ExecuteScript("return window.connectionUp"));

            var currentValue = element.Text;
            Assert.NotEqual(initialValue, currentValue);

            // Verify it continues to tick
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                _ => element.Text != currentValue);
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
