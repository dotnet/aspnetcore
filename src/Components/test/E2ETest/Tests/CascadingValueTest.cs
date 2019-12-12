// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class CascadingValueTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public CascadingValueTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<BasicTestApp.CascadingValueTest.CascadingValueSupplier>();
        }

        [Fact]
        public void CanUpdateValuesMatchedByType()
        {
            var currentCount = Browser.FindElement(By.Id("current-count"));
            var incrementButton = Browser.FindElement(By.Id("increment-count"));

            // We have the correct initial value
            Browser.Equal("100", () => currentCount.Text);

            // Updates are propagated
            incrementButton.Click();
            Browser.Equal("101", () => currentCount.Text);
            incrementButton.Click();
            Browser.Equal("102", () => currentCount.Text);

            // Didn't re-render unrelated descendants
            Assert.Equal("1", Browser.FindElement(By.Id("receive-by-interface-num-renders")).Text);
        }

        [Fact]
        public void CanUpdateValuesMatchedByName()
        {
            var currentFlag1Value = Browser.FindElement(By.Id("flag-1"));
            var currentFlag2Value = Browser.FindElement(By.Id("flag-2"));

            Browser.Equal("False", () => currentFlag1Value.Text);
            Browser.Equal("False", () => currentFlag2Value.Text);

            // Observe that the correct cascading parameter updates
            Browser.FindElement(By.Id("toggle-flag-1")).Click();
            Browser.Equal("True", () => currentFlag1Value.Text);
            Browser.Equal("False", () => currentFlag2Value.Text);
            Browser.FindElement(By.Id("toggle-flag-2")).Click();
            Browser.Equal("True", () => currentFlag1Value.Text);
            Browser.Equal("True", () => currentFlag2Value.Text);

            // Didn't re-render unrelated descendants
            Assert.Equal("1", Browser.FindElement(By.Id("receive-by-interface-num-renders")).Text);
        }

        [Fact]
        public void CanUpdateFixedValuesMatchedByInterface()
        {
            var currentCount = Browser.FindElement(By.Id("current-count"));
            var decrementButton = Browser.FindElement(By.Id("decrement-count"));

            // We have the correct initial value
            Browser.Equal("100", () => currentCount.Text);

            // Updates are propagated
            decrementButton.Click();
            Browser.Equal("99", () => currentCount.Text);
            decrementButton.Click();
            Browser.Equal("98", () => currentCount.Text);

            // Didn't re-render descendants
            Assert.Equal("1", Browser.FindElement(By.Id("receive-by-interface-num-renders")).Text);
        }
    }
}
