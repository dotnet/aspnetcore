// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace OpenQA.Selenium.Support.UI;

public class SelectElement
{
    private readonly IElementHandle _elem;

    public SelectElement(IWebElement webElement)
    {
        _elem = webElement.Element;
    }

    public void SelectByValue(string value)
    {
        _elem.SelectOptionAsync(value).Wait();
    }
}
