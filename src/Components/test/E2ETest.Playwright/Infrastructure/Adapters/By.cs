// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;

namespace OpenQA.Selenium;

public class By
{
    private readonly ByType _byType;
    private string CssSelectorValue { get; init; }
    private string ClassNameValue { get; init; }
    private string IdValue { get; init; }
    private string TagNameValue { get; init; }
    private string LinkTextValue { get; init; }
    private string XPathValue { get; init; }

    public enum ByType { CssSelector, TagName, LinkText,
        Id,
        XPath,
        ClassName
    }
    private By(ByType byType)
    {
        _byType = byType;
    }

    public static By CssSelector(string cssSelector)
        => new By(ByType.CssSelector) { CssSelectorValue = cssSelector };

    public static By Id(string id)
        => new By(ByType.Id) { IdValue = id };

    public static By TagName(string tagName)
        => new By(ByType.TagName) { TagNameValue = tagName };

    public static By LinkText(string linkText)
        => new By(ByType.LinkText) { LinkTextValue = linkText };

    public static By XPath(string xpathValue)
        => new By(ByType.XPath) { XPathValue = xpathValue };

    public static By ClassName(string classNameValue)
        => new By(ByType.ClassName) { ClassNameValue = classNameValue };

    private string ToQuerySelector() => _byType switch
    {
        ByType.TagName => TagNameValue,
        ByType.CssSelector => CssSelectorValue,
        ByType.Id => $"#{IdValue}",
        ByType.XPath => $"xpath={XPathValue}",
        ByType.ClassName => $".{ClassNameValue}",
        _ => throw new NotImplementedException(),
    };

    public override string ToString() => _byType switch
    {
        ByType.TagName => $"[Tagname: {TagNameValue}]",
        ByType.Id => $"[Id: {IdValue}]",
        ByType.CssSelector => $"[CssSelector: {CssSelectorValue}]",
        ByType.LinkText => $"[LinkText: {LinkTextValue}]",
        ByType.XPath => $"[XPath: {XPathValue}]",
        ByType.ClassName => $"[ClassName: {ClassNameValue}]",
        _ => throw new NotImplementedException(),
    };

    public Task<IReadOnlyList<IElementHandle>> MatchAsync(IElementHandle elem)
        => MatchAsync(new QuerySelectable(elem));

    public Task<IReadOnlyList<IElementHandle>> MatchAsync(IPage page)
        => MatchAsync(new QuerySelectable(page));

    private async Task<IReadOnlyList<IElementHandle>> MatchAsync(QuerySelectable root)
    {
        if (_byType == ByType.LinkText)
        {
            var links = await root.QuerySelectorAllAsync("a");
            var linksWithText = await Task.WhenAll(links.Select(async l =>
            {
                var text = await l.TextContentAsync();
                return new { Link = l, Text = text };
            }));
            return linksWithText.Where(l => l.Text.Trim() == LinkTextValue).Select(l => l.Link).ToList();
        }

        return await root.QuerySelectorAllAsync(ToQuerySelector());
    }

    // Playwright has both IPage and IElementHandle with QuerySelectorAllAsync, but there's no
    // common base type. This normalizes over them both.
    private readonly struct QuerySelectable
    {
        private readonly IPage _page;
        private readonly IElementHandle _elem;

        public QuerySelectable(IPage page)
            => _page = page ?? throw new ArgumentNullException(nameof(page));

        public QuerySelectable(IElementHandle elem)
            => _elem = elem ?? throw new ArgumentNullException(nameof(elem));

        public Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
            => _page is not null
            ? _page.QuerySelectorAllAsync(selector)
            : _elem.QuerySelectorAllAsync(selector);
    }
}
