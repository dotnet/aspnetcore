// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyConfigurationTest : ServerTestBase<BlazorWasmTestAppFixture<BasicTestApp.Program>>
{
    private IWebElement _appElement;

    public WebAssemblyConfigurationTest(
         BrowserFixture browserFixture,
         BlazorWasmTestAppFixture<BasicTestApp.Program> serverFixture,
         ITestOutputHelper output) :
         base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = "/subdir";
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();

        Navigate(ServerPathBase);
        _appElement = Browser.MountTestComponent<ConfigurationComponent>();
    }

    [Fact]
    public void WebAssemblyConfiguration_Works()
    {
        // Verify values from the default 'appsettings.json' are read.
        Browser.Equal("Default key1-value", () => _appElement.FindElement(By.Id("key1")).Text);

        if (_serverFixture.TestTrimmedOrMultithreadingApps)
        {
            // Verify values overriden by an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Prod key2-value", _appElement.FindElement(By.Id("key2")).Text);

            // Lastly for sanity, make sure values specified in an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Prod key3-value", _appElement.FindElement(By.Id("key3")).Text);
        }
        else
        {
            // Verify values overriden by an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Development key2-value", _appElement.FindElement(By.Id("key2")).Text);

            // Lastly for sanity, make sure values specified in an environment specific 'appsettings.$(Environment).json are read
            Assert.Equal("Development key3-value", _appElement.FindElement(By.Id("key3")).Text);
        }
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
}
