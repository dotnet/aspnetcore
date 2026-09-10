// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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

        // Navigate with query parameter to trigger nested options format
        Navigate($"{ServerPathBase}?nested-options=true");
        Browser.MountTestComponent<ConfigureRuntime>();
    }

    [Fact]
    public void NestedWebAssemblyOptionsAreAccepted()
    {
        var element = Browser.Exists(By.Id("environment"));
        Browser.Equal("true", () => element.Text);
    }
}
