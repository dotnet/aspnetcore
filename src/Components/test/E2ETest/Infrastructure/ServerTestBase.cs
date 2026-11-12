// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public abstract class ServerTestBase<TServerFixture>
    : BrowserTestBase,
    IClassFixture<TServerFixture>
    where TServerFixture : ServerFixture
{
    public string ServerPathBase => "/subdir";

    protected readonly TServerFixture _serverFixture;

    public ServerTestBase(
        BrowserFixture browserFixture,
        TServerFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, output)
    {
        _serverFixture = serverFixture;
    }

    public void Navigate(string relativeUrl)
    {
        Browser.Navigate(_serverFixture.RootUri, relativeUrl);
    }

    public override async Task DisposeAsync()
    {
        EnhancedNavigationTestUtil.CleanEnhancedNavigationSuppression(this);

        // Tests that opt into a different navigation-exception behavior change a process-global cached
        // value that the shared server fixtures keep observing, so it has to be restored per test.
        TestFeatureSwitches.ResetDisableThrowNavigationException();

        await base.DisposeAsync();
    }

    protected override void InitializeAsyncCore()
    {
        // Clear logs - we check these during tests in some cases.
        // Make sure each test starts clean.
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
    }
}
