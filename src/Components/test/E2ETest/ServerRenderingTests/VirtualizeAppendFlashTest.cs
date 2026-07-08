// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class VirtualizeAppendFlashTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    private const string PlaceholderCells = "#repro-scroll-container td.grid-cell-placeholder";
    private const string DataRows = "#repro-scroll-container tr.repro-row:not(.placeholder-row)";

    // Match VirtualizeAppendRepro.razor's seed count.
    private const int InitialItemCount = 600;

    public VirtualizeAppendFlashTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData(null, "append-btn")]
    [InlineData("beginning", "prepend-btn")]
    public void AsyncItemsProvider_DoesNotFlashPlaceholderRows_OnAnchoredEdgeGrowth(string anchor, string growButton)
    {
        var query = anchor is null ? "gate=true" : $"anchor={anchor}&gate=true";
        Navigate($"{ServerPathBase}/virtualize-append?{query}");

        // Wait for the circuit so clicks aren't lost during prerender.
        Browser.Exists(By.Id("interactive-ready"));

        // Load the initial window at the anchored edge: real rows, no placeholders.
        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);

        // Arm the gate (blocks any fetch that advances past the loaded window), then grow the list.
        ClickById("close-gate");
        ClickById(growButton);

        // While any advancing fetch is pending, no placeholder renders and real rows stay on screen.
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.True(() => GetDataRowCount() > 0);

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.True(HasAppendedRowVisible);
    }

    [Fact]
    public void EndAnchoredAppend_IssuesExactlyTwoProviderCalls_AdvancingToTail()
    {
        Navigate($"{ServerPathBase}/virtualize-append");
        Browser.Exists(By.Id("interactive-ready"));

        // Initial render loads the top window (start=0) then, once End-anchored, the tail (start>0).
        Browser.True(() => GetProviderCalls().Any(c => c.Total == InitialItemCount && c.Start > 0));
        Browser.Equal(2, () => GetProviderCalls().Count);

        var initial = GetProviderCalls();
        var tailStart = initial.Where(c => c.Total == InitialItemCount).Max(c => c.Start);
        var batch = GetIntValue("batch-input");

        ClickById("append-btn");

        // Exactly two more calls (total 4) — the fix relocates the tail fetch, it doesn't add one.
        Browser.Equal(4, () => GetProviderCalls().Count);

        var appendCalls = GetProviderCalls().Skip(2).ToList();
        Assert.Equal(2, appendCalls.Count);
        Assert.Equal(InitialItemCount + batch, appendCalls[0].Total);
        Assert.Equal(InitialItemCount + batch, appendCalls[1].Total);

        // First refetches the old window; second advances to the new tail (old tail + batch).
        Assert.Equal(tailStart, appendCalls[0].Start);
        Assert.Equal(tailStart + batch, appendCalls[1].Start);
    }

    private int GetPlaceholderCellCount() => Browser.FindElements(By.CssSelector(PlaceholderCells)).Count;

    private int GetDataRowCount() => Browser.FindElements(By.CssSelector(DataRows)).Count;

    private readonly record struct ProviderCall(int Start, int Count, int Total);

    private List<ProviderCall> GetProviderCalls()
    {
        var list = new List<ProviderCall>();
        foreach (var li in Browser.FindElements(By.CssSelector("#provider-call-log li.pcall")))
        {
            list.Add(new ProviderCall(
                ParseAttr(li, "data-start"),
                ParseAttr(li, "data-count"),
                ParseAttr(li, "data-total")));
        }

        return list;
    }

    private static int ParseAttr(IWebElement el, string name)
        => int.Parse(el.GetAttribute(name), System.Globalization.CultureInfo.InvariantCulture);

    private int GetIntValue(string id)
    {
        var el = Browser.FindElement(By.Id(id));
        return int.Parse(el.GetAttribute("value"), System.Globalization.CultureInfo.InvariantCulture);
    }

    // Appended rows have indices >= the seed count; a visible one proves the tail actually loaded.
    private bool HasAppendedRowVisible()
    {
        foreach (var row in Browser.FindElements(By.CssSelector(DataRows)))
        {
            var match = System.Text.RegularExpressions.Regex.Match(row.Text, @"Log entry (\d+)");
            if (match.Success && int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 600)
            {
                return true;
            }
        }

        return false;
    }

    private void ClickById(string id)
    {
        // Atomic JS query+click to avoid StaleElementReferenceException on re-render.
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("document.getElementById(arguments[0]).click();", id);
    }
}
