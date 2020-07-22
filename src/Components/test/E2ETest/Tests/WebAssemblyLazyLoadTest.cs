// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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
            Browser.MountTestComponent<TestRouterWithLazyAssembly>();
            Browser.Exists(By.Id("blazor-error-ui"));

            var errorUi = Browser.FindElement(By.Id("blazor-error-ui"));
            Assert.Equal("none", errorUi.GetCssValue("display"));
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/23856")]
        public void CanLazyLoadOnRouteChange()
        {
            // Navigate to a page without any lazy-loaded dependencies
            SetUrlViaPushState("/");
            var app = Browser.MountTestComponent<TestRouterWithLazyAssembly>();

            // Ensure that we haven't requested the lazy loaded assembly
            Assert.False(HasLoadedAssembly("Newtonsoft.Json.dll"));

            // Visit the route for the lazy-loaded assembly
            SetUrlViaPushState("/WithLazyAssembly");

            var button = app.FindElement(By.Id("use-package-button"));

            // Now we should have requested the DLL
            Assert.True(HasLoadedAssembly("Newtonsoft.Json.dll"));

            button.Click();

            // We shouldn't get any errors about assemblies not being available
            AssertLogDoesNotContainCriticalMessages("Could not load file or assembly 'Newtonsoft.Json");
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/23856")]
        public void CanLazyLoadOnFirstVisit()
        {
            // Navigate to a page with lazy loaded assemblies for the first time
            SetUrlViaPushState("/WithLazyAssembly");
            var app = Browser.MountTestComponent<TestRouterWithLazyAssembly>();

            // Wait for the page to finish loading
            new WebDriverWait(Browser, TimeSpan.FromSeconds(2)).Until(
                driver => driver.FindElement(By.Id("use-package-button")) != null);

            var button = app.FindElement(By.Id("use-package-button"));

            // We should have requested the DLL
            Assert.True(HasLoadedAssembly("Newtonsoft.Json.dll"));

            button.Click();

            // We shouldn't get any errors about assemblies not being available
            AssertLogDoesNotContainCriticalMessages("Could not load file or assembly 'Newtonsoft.Json");
        }

        [Fact]
        public void CanLazyLoadAssemblyWithRoutes()
        {
            // Navigate to a page without any lazy-loaded dependencies
            SetUrlViaPushState("/");
            var app = Browser.MountTestComponent<TestRouterWithLazyAssembly>();

            // Ensure that we haven't requested the lazy loaded assembly
            Assert.False(HasLoadedAssembly("LazyTestContentPackage.dll"));

            // Navigate to the designated route
            SetUrlViaPushState("/WithLazyLoadedRoutes");

            // Wait for the page to finish loading
            new WebDriverWait(Browser, TimeSpan.FromSeconds(2)).Until(
                driver => driver.FindElement(By.Id("lazy-load-msg")) != null);

            // Now the assembly has been loaded
            Assert.True(HasLoadedAssembly("LazyTestContentPackage.dll"));

            var button = app.FindElement(By.Id("go-to-lazy-route"));
            button.Click();

            // Navigating the lazy-loaded route should show its content
            var renderedElement = app.FindElement(By.Id("lazy-page"));
            Assert.True(renderedElement.Displayed);
        }

        [Fact]
        public void ThrowsErrorForUnavailableAssemblies()
        {
            // Navigate to a page with lazy loaded assemblies for the first time
            SetUrlViaPushState("/Other");
            var app = Browser.MountTestComponent<TestRouterWithLazyAssembly>();

            // Should've thrown an error for unhandled error
            var errorUiElem = Browser.Exists(By.Id("blazor-error-ui"), TimeSpan.FromSeconds(10));
            Assert.NotNull(errorUiElem);


            AssertLogContainsCriticalMessages("DoesNotExist.dll must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.");
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
