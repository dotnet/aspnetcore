// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyConfigureRuntimeTest : ServerTestBase<BlazorWasmTestAppFixture<BasicTestApp.Program>>
{
    public WebAssemblyConfigureRuntimeTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = "/subdir";
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();

        Navigate(ServerPathBase);
        Browser.MountTestComponent<ConfigureRuntime>();
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
}
