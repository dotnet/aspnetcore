// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Communication;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public abstract class ServerTestBase<TServerFixture>
    : BrowserTestBase,
    IClassFixture<TServerFixture>
    where TServerFixture : ServerFixture
{
    private string _serverPathBase;
    public string ServerPathBase => _serverPathBase;

    protected readonly TServerFixture _serverFixture;

    public ServerTestBase(
        BrowserFixture browserFixture,
        TServerFixture serverFixture,
        ITestOutputHelper output,
        string serverPathBase = "/subdir")
        : base(browserFixture, output)
    {
        _serverFixture = serverFixture;
        _serverPathBase = serverPathBase;
    }

    public void Navigate(string relativeUrl)
    {
        Browser.Navigate(_serverFixture.RootUri, relativeUrl);
    }

    public override async Task DisposeAsync()
    {
        EnhancedNavigationTestUtil.CleanEnhancedNavigationSuppression(this);
        await base.DisposeAsync();
    }

    protected override void InitializeAsyncCore()
    {
        // Clear logs - we check these during tests in some cases.
        // Make sure each test starts clean.
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
    }

    protected override void GrantTestId()
    {
        EnhancedNavigationTestUtil.GrantTestId(this);
    }
}
