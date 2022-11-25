// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

namespace OpenQA.Selenium;

public static class WebDriverExtensions
{
    public static IReadOnlyList<IWebElement> FindElements(this IWebDriver webDriver, By selector)
    {
        return selector.MatchAsync(webDriver.CurrentPage).Result.Select(elem => new WebElement(elem)).Cast<IWebElement>().ToArray();
    }

    public static IWebElement FindElement(this IWebDriver webDriver, By by)
    {
        // TODO: Is it really this, or should it be SingleByDefault? Need to check Selenium semantics.
        return webDriver.FindElements(by).FirstOrDefault();
    }
}
