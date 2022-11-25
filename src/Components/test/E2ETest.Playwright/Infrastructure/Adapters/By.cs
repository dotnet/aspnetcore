// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace OpenQA.Selenium;

public class By
{
    private readonly ByType _byType;
    private string CssSelectorValue { get; init; }
    private string TagNameValue { get; init; }
    private string LinkTextValue { get; init; }

    public enum ByType { CssSelector, TagName, LinkText };

    private By(ByType byType)
    {
        _byType = byType;
    }

    public static By CssSelector(string cssSelector)
        => new By(ByType.CssSelector) { CssSelectorValue = cssSelector };

    public static By TagName(string tagName)
        => new By(ByType.TagName) { TagNameValue = tagName };

    public static By LinkText(string linkText)
        => new By(ByType.LinkText) { LinkTextValue = linkText };

    private string ToQuerySelector() => _byType switch
    {
        ByType.TagName => TagNameValue,
        ByType.CssSelector => CssSelectorValue,
        _ => throw new NotImplementedException(),
    };

    public override string ToString() => _byType switch
    {
        ByType.TagName => $"[Tagname: {TagNameValue}]",
        ByType.CssSelector => $"[CssSelector: {CssSelectorValue}]",
        ByType.LinkText => $"[LinkText: {LinkTextValue}]",
        _ => throw new NotImplementedException(),
    };

    public async Task<IReadOnlyList<IElementHandle>> MatchAsync(IElementHandle elem)
    {
        if (_byType == ByType.LinkText)
        {
            var links = await elem.QuerySelectorAllAsync("a");
            var linksWithText = await Task.WhenAll(links.Select(async l =>
            {
                var text = await l.TextContentAsync();
                return new { Link = l, Text = text };
            }));
            return linksWithText.Where(l => l.Text == LinkTextValue).Select(l => l.Link).ToList();
        }

        return await elem.QuerySelectorAllAsync(ToQuerySelector());
    }

    public async Task<IReadOnlyList<IElementHandle>> MatchAsync(IPage page)
    {
        if (_byType == ByType.LinkText)
        {
            var links = await page.QuerySelectorAllAsync("a");
            var linksWithText = await Task.WhenAll(links.Select(async l =>
            {
                var text = await l.TextContentAsync();
                return new { Link = l, Text = text };
            }));
            return linksWithText.Where(l => l.Text.Trim() == LinkTextValue).Select(l => l.Link).ToList();
        }

        return await page.QuerySelectorAllAsync(ToQuerySelector());
    }
}
