// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Playwright;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;

public class BrowserAdapter : IAsyncDisposable, IJavaScriptExecutor, IWebDriver
{
    private readonly IBrowserContext _browserContext;
    public IPage CurrentPage { get; private set; }

    public BrowserAdapter(IBrowserContext browserContext)
    {
        _browserContext = browserContext;
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.DisposeAsync();
    }

    public void Navigate(Uri rootUri, string relativeUrl, bool noReload)
    {
        if (!noReload || CurrentPage is null)
        {
            CurrentPage?.CloseAsync().Wait();
            CurrentPage = _browserContext.NewPageAsync().Result;
        }

        var destination = new Uri(rootUri, relativeUrl);
        CurrentPage.GotoAsync(destination.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle }).Wait();
    }

    public IReadOnlyList<WebElement> FindElements(By selector)
    {
        return selector.MatchAsync(CurrentPage).Result.Select(elem => new WebElement(elem)).ToArray();
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

        var result = await CurrentPage.EvaluateAsync(script);
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
                _ => throw new NotImplementedException($"Unknown value kind {result.Value.ValueKind}"),
            };
        }

        return null;
    }

    public string Title => CurrentPage.TitleAsync().Result;
}
