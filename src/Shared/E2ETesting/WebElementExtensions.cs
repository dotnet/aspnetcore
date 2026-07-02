// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace OpenQA.Selenium;

public static class WebElementExtensions
{
    // see: https://github.com/seleniumhq/selenium-google-code-issue-archive/issues/214
    //
    // Calling Clear() can trigger onchange, which will revert the value to its default.
    public static void ReplaceText(this IWebElement element, string text, bool clear = true)
    {
        if (clear)
        {
            element.Clear();
        }
        element.SendKeys(Keys.Control + "a" + Keys.Delete);
        element.SendKeys(text);
    }
}
