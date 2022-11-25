// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

namespace OpenQA.Selenium;

public static class WebElementExtensions
{
    public static void Click(this IWebElement webElement)
    {
        webElement.Element.ClickAsync().Wait();
    }

    public static IReadOnlyList<IWebElement> FindElements(this IWebElement webElement, By selector)
    {
        return selector.MatchAsync(webElement.Element).Result.Select(elem => new WebElement(elem)).Cast<IWebElement>().ToArray();
    }

    public static IWebElement FindElement(this IWebElement webElement, By by)
    {
        // TODO: Is it really this, or should it be SingleByDefault? Need to check Selenium semantics.
        return webElement.FindElements(by).FirstOrDefault();
    }

    public static string GetAttribute(this IWebElement webElement, string name)
    {
        // Selenium special-cases 'value'
        // TODO: Only do this for input/textarea/select
        if (string.Equals("value", name, StringComparison.OrdinalIgnoreCase))
        {
            return webElement.Element.InputValueAsync().Result;
        }

        // Selenium special-cases 'innerHTML'
        if (string.Equals("innerHTML", name, StringComparison.OrdinalIgnoreCase))
        {
            return webElement.Element.InnerHTMLAsync().Result;
        }

        try
        {
            return webElement.Element.GetAttributeAsync(name).Result;
        }
        catch (Exception ex) when (ex.InnerException is KeyNotFoundException)
        {
            return null;
        }
    }

    public static void SendKeys(this IWebElement webElement, string keys)
        => webElement.Element.TypeAsync(keys).Wait();

    public static string GetCssValue(this IWebElement webElement, string styleName)
        => webElement.Element.EvaluateAsync($"elem => elem.computedStyleMap().get('{styleName}').toString()").Result.Value.GetString();
}
