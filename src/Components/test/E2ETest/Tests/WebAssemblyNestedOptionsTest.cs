// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

/// <summary>
/// Tests that the blazor.web.js options format (with nested <c>webAssembly:</c> property)
/// is accepted by blazor.webassembly.js.
/// </summary>
public class WebAssemblyNestedOptionsTest : ServerTestBase<BlazorWasmTestAppFixture<BasicTestApp.Program>>
{
    public WebAssemblyNestedOptionsTest(
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

        // Navigate to the page that uses the nested webAssembly options format
        Navigate($"{ServerPathBase}/nestedWebAssemblyOptions.html");
        Browser.MountTestComponent<ConfigureRuntime>();
    }

    [Fact]
    public void NestedWebAssemblyOptionsAreAccepted()
    {
        // Verify that the configureRuntime option inside the nested webAssembly property works
        var element = Browser.Exists(By.Id("environment"));
        Browser.Equal("true", () => element.Text);
    }
}
