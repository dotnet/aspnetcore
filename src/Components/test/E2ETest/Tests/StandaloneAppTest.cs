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

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class StandaloneAppTest
        : ServerTestBase<DevHostServerFixture<StandaloneApp.Program>>, IDisposable
    {
        public StandaloneAppTest(
            BrowserFixture browserFixture, 
            DevHostServerFixture<StandaloneApp.Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
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
            Assert.Equal("Counter", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Counter", item.Text));

            // Verify we can navigate back to home too
            Browser.FindElement(By.LinkText("Home")).Click();
            Assert.Equal("Hello, world!", Browser.FindElement(mainHeaderSelector).Text);
            Assert.Collection(Browser.FindElements(activeNavLinksSelector),
                item => Assert.Equal("Home", item.Text));
        }

        [Fact]
        public void HasCounterPage()
        {
            // Navigate to "Counter"
            Browser.FindElement(By.LinkText("Counter")).Click();
            Assert.Equal("Counter", Browser.FindElement(By.TagName("h1")).Text);

            // Observe the initial value is zero
            var countDisplayElement = Browser.FindElement(By.CssSelector("h1 + p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Click the button; see it counts
            var button = Browser.FindElement(By.CssSelector(".main button"));
            button.Click();
            button.Click();
            button.Click();
            Assert.Equal("Current count: 3", countDisplayElement.Text);
        }

        [Fact]
        public void HasFetchDataPage()
        {
            // Navigate to "Fetch data"
            Browser.FindElement(By.LinkText("Fetch data")).Click();
            Assert.Equal("Weather forecast", Browser.FindElement(By.TagName("h1")).Text);

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
