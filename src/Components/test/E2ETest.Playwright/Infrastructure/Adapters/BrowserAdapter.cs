// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Playwright;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

public class BrowserAdapter : IAsyncDisposable, IJavaScriptExecutor
{
    private readonly IBrowserContext _browserContext;
    private IPage _currentPage;

    public BrowserAdapter(IBrowserContext browserContext)
    {
        _browserContext = browserContext;
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.DisposeAsync();
    }

    internal void Navigate(Uri rootUri, string relativeUrl, bool noReload)
    {
        if (!noReload || _currentPage is null)
        {
            _currentPage?.CloseAsync().Wait();
            _currentPage = _browserContext.NewPageAsync().Result;
        }

        var destination = new Uri(rootUri, relativeUrl);
        _currentPage.GotoAsync(destination.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle }).Wait();
    }

    public WebElement Exists(By selector)
    {
        // TODO: Wait for a match, implement timeout
        var match = selector.MatchAsync(_currentPage).Result.FirstOrDefault();
        if (match is null)
        {
            throw new InvalidOperationException($"No match for {selector}");
        }

        return new WebElement(match);
    }

    public void NotEqual(string expected, Func<string> valueFactory)
    {
        // TODO: Wait for it to be true, implement timeout
        var value = valueFactory();
        Assert.NotEqual(expected, value);
    }

    public IReadOnlyList<WebElement> FindElements(By selector)
    {
        return selector.MatchAsync(_currentPage).Result.Select(elem => new WebElement(elem)).ToArray();
    }

    public object ExecuteScript(string script)
        => ExecuteScriptAsync(script).Result;

    public async Task<object> ExecuteScriptAsync(string script)
    {
        var prefix = "return ";
        if (script.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            script = script.Substring(prefix.Length);
        }

        if (script.EndsWith(";", StringComparison.OrdinalIgnoreCase))
        {
            script = script.Substring(0, script.Length - 1);
        }

        var result = await _currentPage.EvaluateAsync(script);
        if (result.HasValue)
        {
            return result.Value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => result.Value.GetString(),
                JsonValueKind.Number => result.Value.GetDouble(),
                JsonValueKind.Array => result.Value,
                JsonValueKind.Object => result.Value,
                JsonValueKind.Undefined => null,
                JsonValueKind.Null => null,
            };
        }

        return null;
    }

    public string Title => _currentPage.TitleAsync().Result;
}
