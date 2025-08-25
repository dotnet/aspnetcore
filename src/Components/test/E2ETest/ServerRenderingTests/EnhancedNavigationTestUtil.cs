// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public static class EnhancedNavigationTestUtil
{
    public static void SuppressEnhancedNavigation<TServerFixture>(ServerTestBase<TServerFixture> fixture, bool shouldSuppress, bool skipNavigation = false)
        where TServerFixture : ServerFixture
    {
        var browser = fixture.Browser;

        if (!skipNavigation)
        {
            // Navigate here first to ensure the browser is on the correct origin to access sessionStorage
            fixture.Navigate($"{fixture.ServerPathBase}/");
            browser.Equal("Hello", () => browser.Exists(By.TagName("h1")).Text);
        }

        try
        {
            ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.length");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Session storage not found. Ensure that the browser is on the correct origin by navigating to a page or by setting skipNavigation to false.", ex);
        }

        // This prevents test interference where suppression state from previous tests persists
        CleanEnhancedNavigationSuppression(fixture);

        if (shouldSuppress)
        {
            var testId = ((IJavaScriptExecutor)browser).ExecuteScript($"return sessionStorage.getItem('test-id')");
            if (testId is null || string.IsNullOrEmpty(testId as string))
            {
                testId = GrantTestId(browser);
            }

            ((IJavaScriptExecutor)browser).ExecuteScript($"sessionStorage.setItem('suppress-enhanced-navigation-{testId}', 'true')");

            var suppressEnhancedNavigation = ((IJavaScriptExecutor)browser).ExecuteScript($"return sessionStorage.getItem('suppress-enhanced-navigation-{testId}');");
            Assert.True(suppressEnhancedNavigation is not null && (string)suppressEnhancedNavigation == "true",
                "Expected 'suppress-enhanced-navigation' to be set in sessionStorage.");
        }
    }

    public static void CleanEnhancedNavigationSuppression<TServerFixture>(ServerTestBase<TServerFixture> fixture)
        where TServerFixture : ServerFixture
    {
        var browser = fixture.Browser;

        // only tests that suppress enhanced navigation will have these items set, so it can throw
        var testId = ((IJavaScriptExecutor)browser).ExecuteScript($"return sessionStorage.getItem('test-id')");
        if (testId is null || string.IsNullOrEmpty(testId as string))
        {
            return;
        }

        ((IJavaScriptExecutor)browser).ExecuteScript($"sessionStorage.removeItem('test-id')");
        ((IJavaScriptExecutor)browser).ExecuteScript($"sessionStorage.removeItem('suppress-enhanced-navigation-{testId}')");
    }

    private static string GrantTestId(IWebDriver browser)
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        ((IJavaScriptExecutor)browser).ExecuteScript($"sessionStorage.setItem('test-id', '{testId}')");
        return testId;
    }

    public static long GetScrollY(this IWebDriver browser)
        => Convert.ToInt64(((IJavaScriptExecutor)browser).ExecuteScript("return window.scrollY"), CultureInfo.CurrentCulture);

    public static long SetScrollY(this IWebDriver browser, long value)
        => Convert.ToInt64(((IJavaScriptExecutor)browser).ExecuteScript($"window.scrollTo(0, {value})"), CultureInfo.CurrentCulture);
}
