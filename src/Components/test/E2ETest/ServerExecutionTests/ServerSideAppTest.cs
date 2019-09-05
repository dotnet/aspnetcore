// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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
        }

        public DateTime LastLogTimeStamp { get; set; } = DateTime.MinValue;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Capture the last log timestamp so that we can filter logs when we
            // check for duplicate connections.
            var lastLog = Browser.Manage().Logs.GetLog(LogType.Browser).LastOrDefault();
            if (lastLog != null)
            {
                LastLogTimeStamp = lastLog.Timestamp;
            }

            Navigate("/", noReload: false);
            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__started__'];") == null ? false : true);
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Razor Components", Browser.Title);
        }

        [Fact]
        public void DoesNotStartTwoConnections()
        {
            Browser.True(() =>
            {
                var logs = Browser.Manage().Logs.GetLog(LogType.Browser).ToArray();
                var curatedLogs = logs.Where(l => l.Timestamp > LastLogTimeStamp);

                return curatedLogs.Count(e => e.Message.Contains("blazorpack")) == 1;
            });
        }

        [Fact]
        public void HasHeading()
        {
            Browser.Equal("Hello, world!", () => Browser.FindElement(By.CssSelector("h1#index")).Text);
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
            Browser.Equal("Counter", () => Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Counter", item.Text));

            // Verify we can navigate back to home too
            Browser.FindElement(By.LinkText("Home")).Click();
            Browser.Equal("Hello, world!", () => Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));
        }

        [Fact]
        public void HasCounterPage()
        {
            // Navigate to "Counter"
            Browser.FindElement(By.LinkText("Counter")).Click();
            Browser.Equal("Counter", () => Browser.FindElement(By.TagName("h1")).Text);

            // Observe the initial value is zero
            var countDisplayElement = Browser.FindElement(By.CssSelector("h1 + p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Click the button; see it counts
            var button = Browser.FindElement(By.CssSelector(".main button"));
            button.Click();
            Browser.Equal("Current count: 1", () => countDisplayElement.Text);
            button.Click();
            Browser.Equal("Current count: 2", () => countDisplayElement.Text);
            button.Click();
            Browser.Equal("Current count: 3", () => countDisplayElement.Text);
        }

        [Fact]
        public void HasFetchDataPage()
        {
            // Navigate to "Fetch Data"
            Browser.FindElement(By.LinkText("Fetch data")).Click();
            Browser.Equal("Weather forecast", () => Browser.FindElement(By.CssSelector("h1#fetch-data")).Text);

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
            javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

            // We should see the 'reconnecting' UI appear
            var reconnectionDialog = WaitUntilReconnectionDialogExists();
            Browser.True(() => reconnectionDialog.GetCssValue("display") == "block");

            // Then it should disappear
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(driver => reconnectionDialog.GetCssValue("display") == "none");
        }

        [Fact]
        public void RendersContinueAfterReconnect()
        {
            Browser.FindElement(By.LinkText("Ticker")).Click();
            var selector = By.ClassName("tick-value");
            var element = Browser.FindElement(selector);

            var initialValue = element.Text;

            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

            // We should see the 'reconnecting' UI appear
            var reconnectionDialog = WaitUntilReconnectionDialogExists();
            Browser.True(() => reconnectionDialog.GetCssValue("display") == "block");

            // Then it should disappear
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(driver => reconnectionDialog.GetCssValue("display") == "none");

            // We should receive a render that occurred while disconnected
            var currentValue = element.Text;
            Assert.NotEqual(initialValue, currentValue);

            // Verify it continues to tick
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                _ => element.Text != currentValue);
        }

        // Since we've removed stateful prerendering, the name which is passed in
        // during prerendering cannot be retained. The first interactive render
        // will remove it.
        [Fact]
        public void RendersDoNotPreserveState()
        {
            Browser.FindElement(By.LinkText("Greeter")).Click();
            Browser.Equal("Hello", () => Browser.FindElement(By.ClassName("greeting")).Text);
        }

        [Fact]
        public void ErrorsStopTheRenderingProcess()
        {
            Browser.FindElement(By.LinkText("Error")).Click();
            Browser.Equal("Error", () => Browser.FindElement(By.CssSelector("h1")).Text);

            Browser.FindElement(By.Id("cause-error")).Click();
            Browser.True(() => Browser.Manage().Logs.GetLog(LogType.Browser)
                .Any(l => l.Level == LogLevel.Info && l.Message.Contains("Connection disconnected.")));
        }

        private IWebElement WaitUntilReconnectionDialogExists()
        {
            IWebElement reconnectionDialog = null;
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(driver => (reconnectionDialog = driver.FindElement(By.Id("components-reconnect-modal"))) != null);
            return reconnectionDialog;
        }
    }
}
