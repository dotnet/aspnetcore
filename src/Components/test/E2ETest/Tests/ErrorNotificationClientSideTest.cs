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
    public class ErrorNotificationClientSideTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public ErrorNotificationClientSideTest(
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
            Browser.MountTestComponent<ErrorComponent>();
            Browser.Exists(By.Id("error-ui"));
            Browser.Exists(By.TagName("button"));
        }

        [Fact]
        public void ShowsErrorNotificationClientSide_OnError()
        {
            var errorUi = Browser.FindElement(By.Id("error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));

            var causeErrorButton = Browser.FindElement(By.TagName("button"));
            causeErrorButton.Click();

            Browser.Exists(By.CssSelector("#error-ui[style='display: block;']"));

            var reload = Browser.FindElement(By.ClassName("reload"));
            reload.Click();

            Browser.MountTestComponent<ErrorComponent>();
            causeErrorButton = Browser.Exists(By.TagName("button"));
            errorUi = Browser.FindElement(By.Id("error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));

            causeErrorButton.Click();
            Assert.Equal("block", errorUi.GetCssValue("display"));

            var dismiss = Browser.FindElement(By.ClassName("dismiss"));
            dismiss.Click();
            Browser.Exists(By.CssSelector("#error-ui"));
            Browser.Exists(By.CssSelector("#error-ui[style='display: none;']"));
        }
    }
}
