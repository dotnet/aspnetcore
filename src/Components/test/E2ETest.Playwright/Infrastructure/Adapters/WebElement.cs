// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

public class WebElement
{
    private readonly IElementHandle _elem;

    public WebElement(IElementHandle match)
    {
        _elem = match;
    }

    public string Text => _elem.TextContentAsync().Result;

    public void Click()
    {
        _elem.ClickAsync().Wait();
    }

    public IReadOnlyList<WebElement> FindElements(By selector)
    {
        return selector.MatchAsync(_elem).Result.Select(elem => new WebElement(elem)).ToArray();
    }
}
