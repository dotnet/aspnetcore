// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyConfigureRuntimeTest
    : ServerTestBase<BlazorWasmTestAppFixture<Program>>, IDisposable
{
    public WebAssemblyConfigureRuntimeTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/configure-runtime", noReload: true);
        WaitUntilLoaded();
    }

    [Fact]
    public void ConfigureRuntimeWorks()
    {
        var element = Browser.Exists(By.Id("environment"));
        Browser.Equal("true", () => element.Text);
    }

    [Fact]
    public void BlazorRuntimeApiWorks()
    {
        var element = Browser.Exists(By.Id("build-configuration"));
        Browser.Equal("Release", () => element.Text);
    }

    private void WaitUntilLoaded()
    {
        var app = Browser.Exists(By.TagName("app"));
        Browser.NotEqual("Loading...", () => app.Text);
    }

    public void Dispose()
    {
        // Make the tests run faster by navigating back to the home page when we are done
        // If we don't, then the next test will reload the whole page before it starts
        Browser.Exists(By.LinkText("Home")).Click();
    }
}
