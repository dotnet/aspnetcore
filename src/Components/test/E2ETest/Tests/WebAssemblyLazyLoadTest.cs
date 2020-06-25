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
    public class WebAssemblyLazyLoadTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public WebAssemblyLazyLoadTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);
            Browser.MountTestComponent<TestRouter>();
            Browser.Exists(By.Id("blazor-error-ui"));

            var errorUi = Browser.FindElement(By.Id("blazor-error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));
        }

        [Fact]
        public void CanLazyLoadOnRouteChange()
        {
            // Navigate to a page without any lazy-loaded dependencies
            SetUrlViaPushState("/");
            var app = Browser.MountTestComponent<TestRouter>();

            // Ensure that we haven't requested the lazy loaded assembly
            Assert.False(HasLoadedAssembly("Newtonsoft.Json.dll"));

            // Visit the route for the lazy-loaded assembly
            SetUrlViaPushState("/WithDynamicAssembly");

            var button = app.FindElement(By.Id("use-package-button"));

            // Now we should have requested the DLL
            Assert.True(HasLoadedAssembly("Newtonsoft.Json.dll"));

            button.Click();

            // We shouldn't get any errors about assemblies not being available
            AssertLogDoesNotContainCriticalMessages("Could not load file or assembly 'Newtonsoft.Json");
        }

        [Fact]
        public void CanLazyLoadOnFirstVisit()
        {
            // Navigate to a page with lazy loaded assemblies for the first time
            SetUrlViaPushState("/WithDynamicAssembly");
            var app = Browser.MountTestComponent<TestRouter>();
            var button = app.FindElement(By.Id("use-package-button"));

            // We should have requested the DLL
            Assert.True(HasLoadedAssembly("Newtonsoft.Json.dll"));

            button.Click();

            // We shouldn't get any errors about assemblies not being available
            AssertLogDoesNotContainCriticalMessages("Could not load file or assembly 'Newtonsoft.Json");
        }

        private string SetUrlViaPushState(string relativeUri)
        {
            var pathBaseWithoutHash = ServerPathBase.Split('#')[0];
            var jsExecutor = (IJavaScriptExecutor)Browser;
            var absoluteUri = new Uri(_serverFixture.RootUri, $"{pathBaseWithoutHash}{relativeUri}");
            jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.ToString().Replace("'", "\\'")}')");

            return absoluteUri.AbsoluteUri;
        }

        private bool HasLoadedAssembly(string name)
        {
            var checkScript = $"return window.performance.getEntriesByType('resource').some(r => r.name.endsWith('{name}'));";
            var jsExecutor = (IJavaScriptExecutor)Browser;
            var nameRequested = jsExecutor.ExecuteScript(checkScript);
            if (nameRequested != null)
            {
                return (bool)nameRequested;
            }
            return false;
        }

        private void AssertLogDoesNotContainCriticalMessages(params string[] messages)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            foreach (var message in messages)
            {
                Assert.DoesNotContain(log, entry =>
                {
                    return entry.Level == LogLevel.Severe
                    && entry.Message.Contains(message);
                });
            }
        }
    }
}
