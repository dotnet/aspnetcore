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
    public class MonoSanityTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public MonoSanityTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.BuildWebHostMethod = MonoSanity.Program.BuildWebHost;
        }

        protected override void InitializeAsyncCore()
        {
            Navigate("/", noReload: true);
            WaitUntilMonoRunningInBrowser();
        }

        private void WaitUntilMonoRunningInBrowser()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(driver =>
            {
                return ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return window.isTestReady;");
            });
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Mono sanity check", Browser.Title);
        }

        [Fact]
        public void CanAddNumbers()
        {
            SetValue(Browser, "addNumberA", "1001");
            SetValue(Browser, "addNumberB", "2002");
            Browser.FindElement(By.CssSelector("#addNumbers button")).Click();

            Assert.Equal("3003", GetValue(Browser, "addNumbersResult"));
        }

        [Fact]
        public void CanRepeatString()
        {
            SetValue(Browser, "repeatStringStr", "Test");
            SetValue(Browser, "repeatStringCount", "5");
            Browser.FindElement(By.CssSelector("#repeatString button")).Click();

            Assert.Equal("TestTestTestTestTest", GetValue(Browser, "repeatStringResult"));
        }

        [Fact]
        public void CanReceiveDotNetExceptionInJavaScript()
        {
            SetValue(Browser, "triggerExceptionMessage", "Hello from test");
            Browser.FindElement(By.CssSelector("#triggerException button")).Click();

            Assert.Contains("Hello from test", GetValue(Browser, "triggerExceptionMessageStackTrace"));
        }

        [Fact]
        public void CanCallJavaScriptFromDotNet()
        {
            SetValue(Browser, "callJsEvalExpression", "getUserAgentString()");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.StartsWith(".NET received: Mozilla", result);
        }

        [Fact]
        public void CanReceiveJavaScriptExceptionInDotNet()
        {
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
            SetValue(Browser, "callJsEvalExpression", "null");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.Equal(".NET received: (NULL)", result);
        }

        [Fact]
        public void CanEvaluateJsExpressionThatResultsInUndefined()
        {
            SetValue(Browser, "callJsEvalExpression", "console.log('Not returning anything')");
            Browser.FindElement(By.CssSelector("#callJs button")).Click();
            var result = GetValue(Browser, "callJsResult");
            Assert.Equal(".NET received: (NULL)", result);
        }

        [Fact]
        public void CanCallJsFunctionsWithoutBoxing()
        {
            SetValue(Browser, "callJsNoBoxingNumberA", "108");
            SetValue(Browser, "callJsNoBoxingNumberB", "4");
            Browser.FindElement(By.CssSelector("#callJsNoBoxing button")).Click();
            Assert.Equal(".NET received: 27", GetValue(Browser, "callJsNoBoxingResult"));
        }

        [Fact]
        public void CanCallJsFunctionsWithoutBoxingAndReceiveException()
        {
            SetValue(Browser, "callJsNoBoxingNumberA", "1");
            SetValue(Browser, "callJsNoBoxingNumberB", "0");
            Browser.FindElement(By.CssSelector("#callJsNoBoxing button")).Click();

            Assert.StartsWith(".NET got exception: Division by zero", GetValue(Browser, "callJsNoBoxingResult"));
        }

        [Fact]
        public void ReturnsExpectedRuntimeInformation()
        {
            Browser.FindElement(By.CssSelector("#getRuntimeInformation button")).Click();
            Assert.Equal(
                "OSDescription: 'web'; OSArchitecture: 'X86'; IsOSPlatform(WEBASSEMBLY): 'True'",
                GetValue(Browser, "getRuntimeInformationResult"));
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
