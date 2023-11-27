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
        if (shouldSuppress)
        {
            var browser = fixture.Browser;

            if (!skipNavigation)
            {
                // Normally we need to navigate here first otherwise the browser isn't on the correct origin to access
                // localStorage. But some tests are already in the right place and need to avoid extra navigation.
                fixture.Navigate($"{fixture.ServerPathBase}/");
                browser.Equal("Hello", () => browser.Exists(By.TagName("h1")).Text);
            }

            ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }
    }

    public static long GetScrollY(this IWebDriver browser)
        => Convert.ToInt64(((IJavaScriptExecutor)browser).ExecuteScript("return window.scrollY"), CultureInfo.CurrentCulture);
}
