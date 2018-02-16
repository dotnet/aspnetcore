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
    public class HostedInAspNetTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public HostedInAspNetTest(BrowserFixture browserFixture, AspNetSiteServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
            serverFixture.BuildWebHostMethod = HostedInAspNet.Server.Program.BuildWebHost;
            serverFixture.Environment = AspNetEnvironment.Development;
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

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
