// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace Microsoft.AspNetCore.Components.E2ETest;

internal static class WebDriverExtensions
{
    private static string GetFindPositionScript(string elementId) =>
        $"return Math.round(document.getElementById('{elementId}').getBoundingClientRect().top + window.scrollY);";

    public static void Navigate(this IWebDriver browser, Uri baseUri, string relativeUrl)
    {
        var absoluteUrl = new Uri(baseUri, relativeUrl);

        browser.Navigate().GoToUrl("about:blank");
        browser.Navigate().GoToUrl(absoluteUrl);
    }

    public static void WaitForElementToBeVisible(this IWebDriver browser, By by, int timeoutInSeconds = 5)
    {
        var wait = new DefaultWait<IWebDriver>(browser)
        {
            Timeout = TimeSpan.FromSeconds(timeoutInSeconds),
            PollingInterval = TimeSpan.FromMilliseconds(100)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(by);
                return element.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                // Retry finding the element
                return false;
            }
        });
    }

    public static long GetElementPositionWithRetry(this IWebDriver browser, string elementId, int retryCount = 3, int delayBetweenRetriesMs = 100)
    {
        var jsExecutor = (IJavaScriptExecutor)browser;
        string script = GetFindPositionScript(elementId);
        browser.WaitForElementToBeVisible(By.Id(elementId));
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                var result = jsExecutor.ExecuteScript(script);
                if (result != null)
                {
                    return (long)result;
                }
            }
            catch (OpenQA.Selenium.JavaScriptException)
            {
                // JavaScript execution failed, retry
            }

            Thread.Sleep(delayBetweenRetriesMs);
        }

        throw new Exception($"Failed to execute script after {retryCount} retries.");
    }
}
