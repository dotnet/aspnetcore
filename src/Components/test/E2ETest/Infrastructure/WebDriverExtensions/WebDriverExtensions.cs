// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

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
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
        wait.Until(driver =>
        {
            var element = driver.FindElement(by);
            return element.Displayed;
        });
    }
}
