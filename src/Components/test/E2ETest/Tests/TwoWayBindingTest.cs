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
    public class TwoWayBindingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public TwoWayBindingTest(
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
            Browser.MountTestComponent<TwoWayBindingComponent>();
            Browser.Exists(By.Id("two-way-binding"));
        }

        [Fact]
        public void TwoWayBinding_CorrectRowsModified()
        {
            var addRowButton = Browser.Exists(By.Id("addrow"));
            var initial = "I'm a textbox inside a table row.";
            var expected = "I'm changed";

            // add a row.
            addRowButton.Click();
            // add another row.
            addRowButton.Click();

            var boundValue0 = Browser.Exists(By.Id("input-0"));
            var mirrorValue0 = Browser.Exists(By.Id("span-0"));

            var boundValue1 = Browser.Exists(By.Id("input-1"));
            var mirrorValue1 = Browser.Exists(By.Id("span-1"));

            Assert.Equal(initial, boundValue0.GetAttribute("value"));
            Assert.Equal(initial, boundValue1.GetAttribute("value"));
            Assert.Equal(initial, mirrorValue0.Text);
            Assert.Equal(initial, mirrorValue1.Text);

            // Modify target; verify value is updated and that span linked to the same data are updated
            boundValue0.Clear();
            boundValue0.SendKeys($"{expected}\t");
            Assert.Equal(expected, mirrorValue0.Text);
            Assert.Equal(initial, mirrorValue1.Text);

            // Modify other target; verify value is updated and that span linked to the same data are updated
            boundValue1.Clear();
            boundValue1.SendKeys($"{expected}\t");
            Assert.Equal(expected, mirrorValue1.Text);
            Assert.Equal(expected, mirrorValue1.Text);
        }
    }
}
