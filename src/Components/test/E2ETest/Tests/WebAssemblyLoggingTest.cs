// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class WebAssemblyLoggingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public WebAssemblyLoggingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);
            Browser.MountTestComponent<ErrorComponent>();
            Browser.Exists(By.Id("blazor-error-ui"));

            var errorUi = Browser.FindElement(By.Id("blazor-error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));
        }

        [Fact]
        public void LogsSimpleExceptionsUsingLogger()
        {
            Browser.FindElement(By.Id("throw-simple-exception")).Click();
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));
            AssertLogContainsCriticalMessages(
                "crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]",
                "[Custom logger] Unhandled exception rendering component: Doing something that won't work!",
                "System.InvalidTimeZoneException: Doing something that won't work!",
                "at BasicTestApp.ErrorComponent.ThrowSimple");
        }

        [Fact]
        public void LogsInnerExceptionsUsingLogger()
        {
            Browser.FindElement(By.Id("throw-inner-exception")).Click();
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));
            AssertLogContainsCriticalMessages(
                "crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]",
                "[Custom logger] Unhandled exception rendering component: Here is the outer exception",
                "System.InvalidTimeZoneException: Here is the outer exception",
                "System.ArithmeticException: Here is the inner exception",
                "at BasicTestApp.ErrorComponent.ThrowInner");
        }

        [Fact]
        public void LogsAggregateExceptionsUsingLogger()
        {
            Browser.FindElement(By.Id("throw-aggregate-exception")).Click();
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));
            AssertLogContainsCriticalMessages(
                "crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]",
                "[Custom logger] Unhandled exception rendering component: Aggregate exception 1",
                "System.InvalidTimeZoneException: Aggregate exception 1",
                "[Custom logger] Unhandled exception rendering component: Aggregate exception 2",
                "System.InvalidTimeZoneException: Aggregate exception 2",
                "[Custom logger] Unhandled exception rendering component: Aggregate exception 3",
                "System.InvalidTimeZoneException: Aggregate exception 3");
        }

        [Fact]
        public void LogsUsingCustomLogger()
        {
            Browser.MountTestComponent<LoggingComponent>();
            Browser.Exists(By.Id("blazor-error-ui"));
            Browser.Exists(By.Id("log-trace"));

            ((IJavaScriptExecutor)Browser).ExecuteScript("console.info('Test log message')");

            // None of these severity levels are displayed by default, so at the end
            // we'll continue to see "Test log message" as the most recent output
            Browser.FindElement(By.Id("log-none")).Click();
            Browser.FindElement(By.Id("log-trace")).Click();
            Browser.FindElement(By.Id("log-debug")).Click();
            Browser.FindElement(By.Id("log-information")).Click();
            // The Warning minimum log-level is only set on the PrependMessage
            // logger so the last info log will be processed by the default
            // logger but not the PrependMessage one.
            AssertLastLogMessage(LogLevel.Info, "info: BasicTestApp.ErrorComponent[0]");

            // These severity levels are displayed
            Browser.FindElement(By.Id("log-warning")).Click();
            AssertLastLogMessage(LogLevel.Warning, "[Custom logger] This is a Warning message with count=5");
            Browser.FindElement(By.Id("log-error")).Click();
            AssertLastLogMessage(LogLevel.Severe, "[Custom logger] This is a Error message with count=6");

            // All the preceding levels don't cause the error UI to appear
            var errorUi = Browser.FindElement(By.Id("blazor-error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));

            // ... but "Critical" level does
            Browser.FindElement(By.Id("log-critical")).Click();
            AssertLastLogMessage(LogLevel.Severe, "[Custom logger] This is a Critical message with count=7");
            Assert.Equal("block", errorUi.GetCssValue("display"));
        }

        void AssertLastLogMessage(LogLevel level, string message)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            var lastEntry = log[log.Count - 1];
            Assert.Equal(level, lastEntry.Level);

            // Selenium prefixes the message with various bits of internal info, so use "Contains"
            Assert.Contains(message, lastEntry.Message);
        }

        void AssertLogContainsCriticalMessages(params string[] messages)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            foreach (var message in messages)
            {
                Assert.Contains(log, entry =>
                {
                    return entry.Level == LogLevel.Severe
                    && entry.Message.Contains(message);
                });
            }
        }
    }
}
