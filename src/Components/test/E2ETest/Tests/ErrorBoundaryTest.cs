// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using BasicTestApp.ErrorBoundaryTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class ErrorBoundaryTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public ErrorBoundaryTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // Many of these tests trigger fatal exceptions, so we always have to reload
            Navigate(ServerPathBase, noReload: false);
            Browser.MountTestComponent<ErrorBoundaryContainer>();
        }

        [Theory]
        [InlineData("event-sync")]
        [InlineData("event-async")]
        [InlineData("parametersset-sync")]
        [InlineData("parametersset-async")]
        [InlineData("parametersset-cascade-sync")]
        [InlineData("parametersset-cascade-async")]
        [InlineData("afterrender-sync")]
        [InlineData("afterrender-async")]
        [InlineData("while-rendering")]
        public void CanHandleExceptions(string triggerId)
        {
            var container = Browser.Exists(By.Id("error-boundary-container"));
            container.FindElement(By.Id(triggerId)).Click();

            // The whole UI within the container is replaced by the default error UI
            Browser.Collection(() => container.FindElements(By.CssSelector("*")),
                elem =>
                {
                    Assert.Equal("blazor-error-boundary", elem.GetAttribute("class"));
                    Assert.Empty(elem.FindElements(By.CssSelector("*")));
                });

            // It wasn't a fatal error
            var globalErrorUi = Browser.Exists(By.Id("blazor-error-ui"));
            Assert.Equal("none", globalErrorUi.GetCssValue("display"));
        }
    }
}
