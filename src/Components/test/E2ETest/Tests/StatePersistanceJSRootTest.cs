// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

// These tests are for Blazor Web implementation
// For Blazor Server and Webassembly, check SaveStateTest.cs
public class StatePersistanceJSRootTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public StatePersistanceJSRootTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--RegisterDynamicJSRootComponent", "true");
    }

    [Theory]
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void PersistentStateIsSupportedInDynamicJSRoots(string renderMode)
    {
        Navigate($"subdir/WasmMinimal/dynamic-js-root.html?renderMode={renderMode}");

        Browser.Equal("Counter", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("Current count: 0", () => Browser.Exists(By.CssSelector("p[role='status']")).Text);

        Browser.Click(By.CssSelector("button.btn-primary"));
        Browser.Equal("Current count: 1", () => Browser.Exists(By.CssSelector("p[role='status']")).Text);
    }
}
