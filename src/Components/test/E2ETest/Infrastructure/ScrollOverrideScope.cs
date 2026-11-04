// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenQA.Selenium;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

internal sealed class ScrollOverrideScope : IDisposable
{
    private readonly IJavaScriptExecutor _executor;
    private readonly bool _isActive;

    public ScrollOverrideScope(IWebDriver browser, bool isActive)
    {
        _executor = (IJavaScriptExecutor)browser;
        _isActive = isActive;

        if (!_isActive)
        {
            return;
        }

        _executor.ExecuteScript(@"
(function() {
    if (window.__enhancedNavScrollOverride) {
        if (window.__clearEnhancedNavScrollLog) {
            window.__clearEnhancedNavScrollLog();
        }
        return;
    }

    const original = window.scrollTo.bind(window);
    const log = [];

    function resolvePage() {
        const landing = document.getElementById('test-info-1');
        if (landing && landing.textContent === 'Scroll tests landing page') {
            return 'landing';
        }

        const next = document.getElementById('test-info-2');
        if (next && next.textContent === 'Scroll tests next page') {
            return 'next';
        }

        return 'other';
    }

    window.__enhancedNavScrollOverride = true;
    window.__enhancedNavOriginalScrollTo = original;
    window.__enhancedNavScrollLog = log;
    window.__clearEnhancedNavScrollLog = () => { log.length = 0; };
    window.__drainEnhancedNavScrollLog = () => {
        const copy = log.slice();
        log.length = 0;
        return copy;
    };

    window.scrollTo = function(...args) {
        log.push({
            page: resolvePage(),
            url: location.href,
            time: performance.now(),
            args
        });

        return original(...args);
    };
})();
");

        ClearLog();
    }

    public void ClearLog()
    {
        if (!_isActive)
        {
            return;
        }

        _executor.ExecuteScript("if (window.__clearEnhancedNavScrollLog) { window.__clearEnhancedNavScrollLog(); }");
    }

    public void AssertNoPrematureScroll(string expectedPage, string navigationDescription)
    {
        if (!_isActive)
        {
            return;
        }

        var entries = DrainLog();
        if (entries.Length == 0)
        {
            return;
        }

        var unexpectedEntries = entries
            .Where(entry => !string.Equals(entry.Page, expectedPage, StringComparison.Ordinal))
            .ToArray();

        if (unexpectedEntries.Length == 0)
        {
            return;
        }

        var details = string.Join(
            ", ",
            unexpectedEntries.Select(entry => $"page={entry.Page ?? "null"} url={entry.Url} time={entry.Time:F2}"));

        throw new XunitException($"Detected a scroll reset while the DOM still displayed '{unexpectedEntries[0].Page ?? "unknown"}' during {navigationDescription}. Entries: {details}");
    }

    private ScrollInvocation[] DrainLog()
    {
        if (!_isActive)
        {
            return Array.Empty<ScrollInvocation>();
        }

        var result = _executor.ExecuteScript("return window.__drainEnhancedNavScrollLog ? window.__drainEnhancedNavScrollLog() : [];");
        if (result is not IReadOnlyList<object> entries || entries.Count == 0)
        {
            return Array.Empty<ScrollInvocation>();
        }

        var resolved = new ScrollInvocation[entries.Count];
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i] is IReadOnlyDictionary<string, object> dict)
            {
                dict.TryGetValue("page", out var pageValue);
                dict.TryGetValue("url", out var urlValue);
                dict.TryGetValue("time", out var timeValue);

                resolved[i] = new ScrollInvocation(
                    pageValue as string,
                    urlValue as string,
                    timeValue is null ? 0D : Convert.ToDouble(timeValue, CultureInfo.InvariantCulture));
                continue;
            }

            resolved[i] = new ScrollInvocation(null, null, 0D);
        }

        return resolved;
    }

    public void Dispose()
    {
        if (!_isActive)
        {
            return;
        }

        _executor.ExecuteScript(@"
(function() {
    if (!window.__enhancedNavScrollOverride) {
        return;
    }

    if (window.__enhancedNavOriginalScrollTo) {
        window.scrollTo = window.__enhancedNavOriginalScrollTo;
        delete window.__enhancedNavOriginalScrollTo;
    }

    delete window.__enhancedNavScrollOverride;
    delete window.__enhancedNavScrollLog;
    delete window.__clearEnhancedNavScrollLog;
    delete window.__drainEnhancedNavScrollLog;
})();
");
    }

    private readonly record struct ScrollInvocation(string Page, string Url, double Time);
}
