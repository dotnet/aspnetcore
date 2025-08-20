// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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

    protected override void InitializeAsyncCore(bool supportEnhancedNavigationSuppression = false)
    {
        if (supportEnhancedNavigationSuppression)
        {
            GrantTestId();
        }

        // Clear logs - we check these during tests in some cases.
        // Make sure each test starts clean.
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");

        InitializeAsyncCore();
    }

    protected override void InitializeAsyncCore()
    {
    }

    private void GrantTestId()
    {
        // We have to be on any page to ensure session storage is available
        // Since PageLoadStrategy can be set to None, we need to manually wait for page load
        Navigate(ServerPathBase);

        // Wait for the page to be completely loaded (equivalent to PageLoadStrategy.Normal)
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Browser, TimeSpan.FromSeconds(10));
        wait.Until(driver =>
        {
            try
            {
                // Check if document is ready and load event has fired
                var loadComplete = ((IJavaScriptExecutor)driver).ExecuteScript(
                    "return document.readyState === 'complete' && performance.timing.loadEventEnd > 0");
                return (bool)loadComplete;
            }
            catch
            {
                return false;
            }
        });

        var testId = Guid.NewGuid().ToString("N")[..8];
        ((IJavaScriptExecutor)Browser).ExecuteScript($"sessionStorage.setItem('test-id', '{testId}')");
    }
}
