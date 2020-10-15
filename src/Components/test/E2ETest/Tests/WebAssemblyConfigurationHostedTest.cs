// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class WebAssemblyConfigurationHostedTest : ServerTestBase<BasicTestAppServerSiteFixture<TestServer.ClientStartup>>
    {
        private IWebElement _appElement;

        public WebAssemblyConfigurationHostedTest(
             BrowserFixture browserFixture,
             BasicTestAppServerSiteFixture<TestServer.ClientStartup> serverFixture,
             ITestOutputHelper output) :
             base(browserFixture, serverFixture, output)
        {
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
            Assert.Equal("Prod key2-value", _appElement.FindElement(By.Id("key2")).Text);

            // Lastly for sanity, make sure values specified in an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Prod key3-value", _appElement.FindElement(By.Id("key3")).Text);
        }

        [Fact]
        public void WebAssemblyHostingEnvironment_Works()
        {
            // Verify values from the default 'appsettings.json' are read.
            Browser.Equal("Production", () => _appElement.FindElement(By.Id("environment")).Text);
        }
    }
}
