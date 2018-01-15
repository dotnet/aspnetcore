// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.E2ETest.Infrastructure;
using Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
{
    public class StandaloneAppTest
        : ServerTestBase<DevHostServerFixture<StandaloneApp.Program>>
    {
        public StandaloneAppTest(BrowserFixture browserFixture, DevHostServerFixture<StandaloneApp.Program> serverFixture)
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
        public void HasBodyText()
        {
            var bodyText = Browser.FindElement(By.TagName("body")).Text;
            Assert.Equal("Hello, world!", bodyText);
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
