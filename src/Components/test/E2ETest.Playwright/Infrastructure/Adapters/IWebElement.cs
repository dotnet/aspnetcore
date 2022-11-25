// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;
using Microsoft.Playwright;

namespace OpenQA.Selenium;

public interface IWebElement
{
    IElementHandle Element { get; }

    public string Text => new WebElement(Element).Text;
}
