// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class DelegateEventBindingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public DelegateEventBindingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<DelegateEventBindingComponent>();
            Browser.Exists(By.Id("delegate-event-binding"));
        }

        [Fact]
        public void DelegateEventBinding_CorrectRowsChanged()
        {
            var addRowButton = Browser.Exists(By.Id("addrow"));
            var initial = "I'm a textbox inside a table row.";
            var expected = "I'm changed";

            // add a three rows.
            addRowButton.Click();
            addRowButton.Click();
            addRowButton.Click();

            var target0 = Browser.Exists(By.Id("input-0"));
            var target1 = Browser.Exists(By.Id("input-1"));
            var target2 = Browser.Exists(By.Id("input-2"));

            var lastChangedValue = Browser.Exists(By.Id("last-value"));

            Assert.Equal(initial, target0.GetAttribute("value"));
            Assert.Equal(initial, target1.GetAttribute("value"));
            Assert.Equal(initial, target2.GetAttribute("value"));
            Assert.Equal(string.Empty, lastChangedValue.Text);

            // Modify target; verify value is updated and that last value is updated.
            target0.Clear();
            target0.SendKeys($"{expected}\t");

            Assert.Equal($"0: {expected}", lastChangedValue.Text);

            // Modify target; verify value is updated and that last value is updated.
            target1.Clear();
            target1.SendKeys($"{expected}\t");

            Assert.Equal($"1: {expected}", lastChangedValue.Text);

            // Modify target; verify value is updated and that last value is updated.
            target2.Clear();
            target2.SendKeys($"{expected}\t");

            Assert.Equal($"2: {expected}", lastChangedValue.Text);
        }
    }
}
