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
    private static bool _isSuppressed;

    public static void SuppressEnhancedNavigation<TServerFixture>(ServerTestBase<TServerFixture> fixture, bool shouldSuppress, bool skipNavigation = false)
        where TServerFixture : ServerFixture
    {
        if (shouldSuppress)
        {
            var browser = fixture.Browser;

            if (!skipNavigation)
            {
                NavigateToOrigin(fixture);
            }

            try
            {
                ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.length");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Session storage not found. Ensure that the browser is on the correct origin by navigating to a page or by setting skipNavigation to false.", ex);
            }

            ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");

            var suppressEnhancedNavigation = ((IJavaScriptExecutor)browser).ExecuteScript("return sessionStorage.getItem('suppress-enhanced-navigation');");
            Assert.True(suppressEnhancedNavigation is not null && (string)suppressEnhancedNavigation == "true",
                "Expected 'suppress-enhanced-navigation' to be set in sessionStorage.");
            _isSuppressed = true;
        }
    }

    public static void CleanEnhancedNavigationSuppression<TServerFixture>(ServerTestBase<TServerFixture> fixture, bool skipNavigation = false)
        where TServerFixture : ServerFixture
    {
        if (!_isSuppressed)
        {
            return;
        }

        var browser = fixture.Browser;

        try
        {
            // First, ensure we're on the correct origin to access sessionStorage
            try
            {
                // Check if we can access sessionStorage from current location
                ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.length");
            }
            catch
            {
                if (skipNavigation)
                {
                    throw new InvalidOperationException("Session storage not found. Ensure that the browser is on the correct origin by navigating to a page or by setting skipNavigation to false.");
                }
                NavigateToOrigin(fixture);
            }
            ((IJavaScriptExecutor)browser).ExecuteScript($"sessionStorage.removeItem('suppress-enhanced-navigation')");
        }
        catch (WebDriverException ex) when (ex.Message.Contains("invalid session id"))
        {
            // Browser session is no longer valid (e.g., browser was closed)
            // Session storage is automatically cleared when browser closes, so cleanup is already done
            // This is expected in some tests, so we silently return
            return;
        }
        finally
        {
            _isSuppressed = false;
        }
    }

    private static void NavigateToOrigin<TServerFixture>(ServerTestBase<TServerFixture> fixture)
        where TServerFixture : ServerFixture
    {
        // Navigate to the test origin to ensure the browser is on the correct state to access sessionStorage
        fixture.Navigate($"{fixture.ServerPathBase}/");
        fixture.Browser.Exists(By.Id("session-storage-marker"));
    }

    public static long GetScrollY(this IWebDriver browser)
        => Convert.ToInt64(((IJavaScriptExecutor)browser).ExecuteScript("return window.scrollY"), CultureInfo.CurrentCulture);

    public static long SetScrollY(this IWebDriver browser, long value)
        => Convert.ToInt64(((IJavaScriptExecutor)browser).ExecuteScript($"window.scrollTo(0, {value})"), CultureInfo.CurrentCulture);
}
