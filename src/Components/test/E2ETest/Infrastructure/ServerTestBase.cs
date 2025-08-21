// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
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
        TryCleanSessionStorage();

        await base.DisposeAsync();
    }

    protected override void InitializeAsyncCore()
    {
        // Clear logs - we check these during tests in some cases.
        // Make sure each test starts clean.
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
    }

    private void TryCleanSessionStorage()
    {
        try
        {
            // only tests that suppress enhanced navigation will have these items set
            var testId = ((IJavaScriptExecutor)Browser).ExecuteScript($"return sessionStorage.getItem('test-id')");
            if (testId == null)
            {
                return;
            }

            ((IJavaScriptExecutor)Browser).ExecuteScript($"sessionStorage.removeItem('test-id')");
            ((IJavaScriptExecutor)Browser).ExecuteScript($"sessionStorage.removeItem('suppress-enhanced-navigation-{testId}')");
        }
        catch (Exception ex)
        {
            // exception here most probably means session storage is not available - no cleanup needed
            Output?.WriteLine("Error when removing test id from session storage: " + ex.Message);
        }
    }
}
