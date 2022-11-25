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
