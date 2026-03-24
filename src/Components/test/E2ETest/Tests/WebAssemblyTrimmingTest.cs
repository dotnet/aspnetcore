// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyTrimmingTest : ServerTestBase<BlazorWasmTestAppFixture<Program>>
{
    public WebAssemblyTrimmingTest(
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
    }

    [Fact]
    public void MetricsTypesAreTrimmed_WhenPublishedWithTrimming()
    {
        if (!_serverFixture.TestTrimmedOrMultithreadingApps)
        {
            // In dev mode, metrics types are expected to be present
            return;
        }

        var appElement = Browser.MountTestComponent<MetricsTrimmingCheck>();

        // Verify that MetricsManager type has been trimmed away
        Browser.Equal("false", () => appElement.FindElement(By.Id("metrics-found")).Text);
    }
}
