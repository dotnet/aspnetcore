// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Microsoft.AspNetCore.Components.E2ETest;

internal static class WebDriverExtensions
{
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
        string log = "";

        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                browser.WaitForElementToBeVisible(By.Id(elementId));
                var element = browser.FindElement(By.Id(elementId));
                return element.Location.Y;
            }
            catch (Exception ex)
            {
                log += $"Attempt {i + 1}: - {ex.Message}. ";
            }

            if (i < retryCount - 1)
            {
                Thread.Sleep(delayBetweenRetriesMs);
            }
        }

        throw new Exception($"Failed to get position for element '{elementId}' after {retryCount} retries. Debug log: {log}");
    }
}
