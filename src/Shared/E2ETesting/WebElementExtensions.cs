// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace OpenQA.Selenium
{
    public static class WebElementExtensions
    {
        // see: https://github.com/seleniumhq/selenium-google-code-issue-archive/issues/214
        //
        // Calling Clear() can trigger onchange, which will revert the value to its default.
        public static void ReplaceText(this IWebElement element, string text)
        {
            element.SendKeys(Keys.Control + "a");
            element.SendKeys(text);
        }
    }
}
