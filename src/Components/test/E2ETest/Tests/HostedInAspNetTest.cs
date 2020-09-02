// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class HostedInAspNetTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public HostedInAspNetTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.BuildWebHostMethod = HostedInAspNet.Server.Program.BuildWebHost;
            serverFixture.Environment = AspNetEnvironment.Development;
        }

        protected override void InitializeAsyncCore()
        {
            Navigate("/", noReload: true);
            WaitUntilLoaded();
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Sample Blazor app", Browser.Title);
        }

        [Fact]
        public void ServesStaticAssetsFromClientAppWebRoot()
        {
            var javascriptExecutor = (IJavaScriptExecutor)Browser;
            var bootstrapTooltipType = javascriptExecutor
                .ExecuteScript("return window.customJsWasLoaded;");
            Assert.True((bool)bootstrapTooltipType);
        }

        [Fact]
        public void SignalRClientWorks()
        {
            var mainHeaderSelector = By.TagName("h1");

            // Verify we start at home, with the home link highlighted
            Assert.Equal("Hello, world!", Browser.FindElement(mainHeaderSelector).Text);

            // Navigate to "signalr"
            Browser.FindElement(By.LinkText("SignalR")).Click();
            Assert.Equal("SignalR!", Browser.FindElement(mainHeaderSelector).Text);

            var messagesElement = Browser.FindElement(By.CssSelector("li"));
            Assert.Equal("SignalR Client: Echo", messagesElement.Text);
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
