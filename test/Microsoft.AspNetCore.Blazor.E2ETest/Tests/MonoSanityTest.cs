// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class MonoSanityTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public MonoSanityTest(BrowserFixture browserFixture, AspNetSiteServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
            serverFixture.BuildWebHostMethod = MonoSanity.Program.BuildWebHost;
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

        [Fact]
        public void CanReceiveDotNetExceptionInJavaScript()
        {
            Navigate("/", noReload: true);

            SetValue(Browser, "triggerExceptionMessage", "Hello from test");
            Browser.FindElement(By.CssSelector("#triggerException button")).Click();

            Assert.Contains("Hello from test", GetValue(Browser, "triggerExceptionMessageStackTrace"));
        }

        [Fact]
        public void CanCallJavaScriptFromDotNet()
        {
            Navigate("/", noReload: true);
            SetValue(Browser, "callJsEvalExpression", "getUserAgentString()");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.StartsWith(".NET received: Mozilla", result);
        }

        [Fact]
        public void CanReceiveJavaScriptExceptionInDotNet()
        {
            Navigate("/", noReload: true);
            SetValue(Browser, "callJsEvalExpression", "triggerJsException()");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.StartsWith(".NET got exception: This is a JavaScript exception.", result);

            // Also verify we got a stack trace
            Assert.Contains("at triggerJsException", result);
        }

        [Fact]
        public void CanEvaluateJsExpressionThatResultsInNull()
        {
            Navigate("/", noReload: true);
            SetValue(Browser, "callJsEvalExpression", "null");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.Equal(".NET received: (NULL)", result);
        }

        [Fact]
        public void CanEvaluateJsExpressionThatResultsInUndefined()
        {
            Navigate("/", noReload: true);
            SetValue(Browser, "callJsEvalExpression", "console.log('Not returning anything')");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.Equal(".NET received: (NULL)", result);
        }

        [Fact]
        public void CanCallJsFunctionsWithoutBoxing()
        {
            Navigate("/", noReload: true);

            SetValue(Browser, "callJsNoBoxingNumberA", "108");
            SetValue(Browser, "callJsNoBoxingNumberB", "4");
            Browser.FindElement(By.CssSelector("#callJsNoBoxing button")).Click();

            Assert.Equal(".NET received: 27", GetValue(Browser, "callJsNoBoxingResult"));
        }

        [Fact]
        public void CanCallJsFunctionsWithoutBoxingAndReceiveException()
        {
            Navigate("/", noReload: true);

            SetValue(Browser, "callJsNoBoxingNumberA", "1");
            SetValue(Browser, "callJsNoBoxingNumberB", "0");
            Browser.FindElement(By.CssSelector("#callJsNoBoxing button")).Click();

            Assert.StartsWith(".NET got exception: Division by zero", GetValue(Browser, "callJsNoBoxingResult"));
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
