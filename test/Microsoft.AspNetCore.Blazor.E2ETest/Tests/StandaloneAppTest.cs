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
        : ServerTestBase<DevHostServerFixture<StandaloneApp.ProgramY>>
    {
        public StandaloneAppTest(BrowserFixture browserFixture, DevHostServerFixture<StandaloneApp.ProgramY> serverFixture)
            : base(browserFixture, serverFixture)
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
        public void ServesStaticAssetsFromClientAppWebRoot()
        {
            // Verify that bootstrap.js was loaded
            var javascriptExecutor = (IJavaScriptExecutor)Browser;
            var bootstrapTooltipType = javascriptExecutor
                .ExecuteScript("return typeof (window.Tooltip);");
            Assert.Equal("function", bootstrapTooltipType);
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
