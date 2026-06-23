// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class BrowserConfigurationTest(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>>>(browserFixture, serverFixture, output)
{
    [Fact]
    public void CanReceiveEnvironmentVariablesFromBrowserConfiguration()
    {
        Navigate($"{ServerPathBase}/browser-configuration-env-vars");

        // Wait for WebAssembly to be interactive
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Verify environment variables set via WithBrowserConfiguration are available
        Browser.Equal("test-value-from-server", () => Browser.Exists(By.Id("my-test-var")).Text);
        Browser.Equal("another-test-value", () => Browser.Exists(By.Id("another-test-var")).Text);
    }

    [Fact]
    public void CanReceiveEnvironmentVariablesFromBrowserConfiguration_AfterEnhancedNavigation()
    {
        // Start on a static (SSR) page where WebAssembly is not running
        Navigate($"{ServerPathBase}/browser-configuration-env-vars-landing");
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Navigate to the WASM page via enhanced navigation (clicking a link)
        Browser.Click(By.Id("navigate-to-env-vars"));

        // Wait for WebAssembly to be interactive
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Verify environment variables are available even though WASM started via enhanced navigation
        Browser.Equal("test-value-from-server", () => Browser.Exists(By.Id("my-test-var")).Text);
        Browser.Equal("another-test-value", () => Browser.Exists(By.Id("another-test-var")).Text);
    }
}
