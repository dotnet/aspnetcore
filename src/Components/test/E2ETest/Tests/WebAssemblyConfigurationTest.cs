// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class WebAssemblyConfigurationTest : ServerTestBase<DevHostServerFixture<BasicTestApp.Program>>
    {
        private IWebElement _appElement;

        public WebAssemblyConfigurationTest(
             BrowserFixture browserFixture,
             DevHostServerFixture<BasicTestApp.Program> serverFixture,
             ITestOutputHelper output) :
             base(browserFixture, serverFixture, output)
        {
            _serverFixture.PathBase = "/subdir";
        }

        protected override void InitializeAsyncCore()
        {
            base.InitializeAsyncCore();

            Navigate(ServerPathBase, noReload: false);
            _appElement = Browser.MountTestComponent<ConfigurationComponent>();
        }

        [Fact]
        public void WebAssemblyConfiguration_Works()
        {
            // Verify values from the default 'appsettings.json' are read.
            Browser.Equal("Default key1-value", () => _appElement.FindElement(By.Id("key1")).Text);

            // Verify values overriden by an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Development key2-value", _appElement.FindElement(By.Id("key2")).Text);

            // Lastly for sanity, make sure values specified in an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Development key3-value", _appElement.FindElement(By.Id("key3")).Text);
        }

        [Fact]
        public void WebAssemblyConfiguration_ReloadingWorks()
        {
            // Verify values from the default 'appsettings.json' are read.
            Browser.Equal("Default key1-value", () => _appElement.FindElement(By.Id("key1")).Text);

            // Change the value of key1 using the form in the UI
            var input = _appElement.FindElement(By.Id("key1-input"));
            input.SendKeys("newValue");
            var submit = _appElement.FindElement(By.Id("trigger-change"));
            submit.Click();

            // Asser that the value of the key has been updated
            Browser.Equal("newValue", () => _appElement.FindElement(By.Id("key1")).Text);
        }

        [Fact]
        public void WebAssemblyHostingEnvironment_Works()
        {
            // Dev-Server defaults to Development. It's in the name!
            Browser.Equal("Development", () => _appElement.FindElement(By.Id("environment")).Text);
        }
    }
}
