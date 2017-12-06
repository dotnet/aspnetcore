// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.E2ETest.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
{
    public class MonoSanityTest : AspNetSiteTestBase<MonoSanity.Startup>
    {
        public MonoSanityTest(BrowserFixture browserFixture, AspNetServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
        }

        [Fact]
        public void HasTitle()
        {
            Navigate("/", noReload: true);
            Assert.Equal("Mono sanity check", Browser.Title);
        }

        [Fact]
        public void CanAddNumbers()
        {
            Navigate("/", noReload: true);

            SetValue(Browser, "addNumberA", "1001");
            SetValue(Browser, "addNumberB", "2002");
            Browser.FindElement(By.CssSelector("#addNumbers button")).Click();

            Assert.Equal("3003", GetValue(Browser, "addNumbersResult"));
        }

        [Fact]
        public void CanRepeatString()
        {
            Navigate("/", noReload: true);

            SetValue(Browser, "repeatStringStr", "Test");
            SetValue(Browser, "repeatStringCount", "5");
            Browser.FindElement(By.CssSelector("#repeatString button")).Click();

            Assert.Equal("TestTestTestTestTest", GetValue(Browser, "repeatStringResult"));
        }

        private static string GetValue(IWebDriver webDriver, string elementId)
        {
            var element = webDriver.FindElement(By.Id(elementId));
            return element.GetAttribute("value");
        }

        private static void SetValue(IWebDriver webDriver, string elementId, string value)
        {
            var element = webDriver.FindElement(By.Id(elementId));
            element.Clear();
            element.SendKeys(value);
        }
    }
}
