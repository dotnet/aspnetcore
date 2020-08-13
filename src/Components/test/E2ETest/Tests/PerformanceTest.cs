// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class PerformanceTest
        : ServerTestBase<DevHostServerFixture<Wasm.Performance.TestApp.Program>>
    {
        public PerformanceTest(
            BrowserFixture browserFixture,
            DevHostServerFixture<Wasm.Performance.TestApp.Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate("/", noReload: true);
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("E2EPerformance", Browser.Title);
        }

        [Fact]
        public void BenchmarksRunWithoutError()
        {
            // In CI, we only verify that the benchmarks run without throwing any
            // errors. To get actual perf numbers, you must run the E2EPerformance
            // site manually.
            var verifyOnlyLabel = Browser.FindElement(By.XPath("//label[contains(text(), 'Verify only')]/input"));
            verifyOnlyLabel.Click();

            var runAllButton = Browser.FindElement(By.CssSelector("button.btn-success.run-button"));
            runAllButton.Click();

            // The "run" button goes away while the benchmarks execute, then it comes back
            Browser.False(() => runAllButton.Displayed);
            Browser.True(
                () => runAllButton.Displayed || Browser.FindElements(By.CssSelector(".benchmark-error")).Any(),
                TimeSpan.FromSeconds(60));

            Browser.DoesNotExist(By.CssSelector(".benchmark-error")); // no failures
            Browser.Exists(By.CssSelector(".benchmark-idle")); // everything's done
        }
    }
}
