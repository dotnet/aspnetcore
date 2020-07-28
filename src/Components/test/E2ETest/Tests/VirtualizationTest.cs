// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests
{
    public class VirtualizationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public VirtualizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<VirtualizationComponent>();
        }

        [Fact]
        public void VirtualizeFixed_CanRenderStacked()
        {
            var container = Browser.FindElement(By.Id("stacked-container"));

            Browser.True(() => Browser.FindElements(By.Id("stacked-top-item")).Count > 0);
            Browser.True(() => Browser.FindElements(By.Id("stacked-bottom-item")).Count == 0);

            Browser.ExecuteJavaScript("const container = document.getElementById('stacked-container');container.scrollTop = container.scrollHeight;");

            Browser.True(() => Browser.FindElements(By.Id("stacked-bottom-item")).Count > 0);
        }

        // TODO: Complete E2E tests.
    }
}
