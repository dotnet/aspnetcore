// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

public class WebElement : IWebElement
{
    public WebElement(IElementHandle match)
    {
        Element = match;
    }

    public IElementHandle Element { get; }

    public string Text => Element.TextContentAsync().Result.Trim(); // Selenium trims
}
