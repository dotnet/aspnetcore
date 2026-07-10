// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class VirtualizationRenderModesTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    private const string PlaceholderCells = "#repro-scroll-container td.grid-cell-placeholder";
    private const string DataRows = "#repro-scroll-container tr.repro-row:not(.placeholder-row)";

    // Match VirtualizeAppendRepro.razor's seed count.
    private const int InitialItemCount = 600;

    public VirtualizationRenderModesTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void Virtualize_Works_WhenMultipleRenderModesAreActive()
    {
        Navigate($"{ServerPathBase}/interactivity/virtualization");

        Browser.Equal("interactive", () => Browser.FindElement(By.Id("virtualize-server")).GetDomAttribute("class"));
        Browser.Equal("interactive", () => Browser.FindElement(By.Id("virtualize-webassembly")).GetDomAttribute("class"));

        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 1"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 1"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 50"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 50"));

        ScrollTopToEnd(Browser, Browser.FindElement(By.Id("virtualize-server")));
        ScrollTopToEnd(Browser, Browser.FindElement(By.Id("virtualize-webassembly")));

        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 1"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 1"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 50"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 50"));
    }

    [Theory]
    [InlineData(null, "append-btn", false)]
    [InlineData(null, "append-btn", true)]
    [InlineData("beginning", "prepend-btn", true)]
    public void AsyncItemsProvider_DoesNotFlashPlaceholderRows_OnAnchoredEdgeGrowth(string anchor, string growButton, bool comparer)
    {
        var parts = new List<string> { "gate=true" };
        if (anchor is not null)
        {
            parts.Add($"anchor={anchor}");
        }
        if (comparer)
        {
            parts.Add("comparer=true");
        }
        Navigate($"{ServerPathBase}/virtualize-append?{string.Join("&", parts)}");

        Browser.Exists(By.Id("interactive-ready"));

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);

        // Arm the gate so any fetch that advances past the loaded window blocks, then grow the list.
        ClickById("close-gate");
        ClickById(growButton);

        // The core assertion: while the advancing fetch is pending, no placeholder flashes.
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.True(() => GetDataRowCount() > 0);

        var batch = GetIntValue("batch-input");

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.Equal(InitialItemCount + batch, GetRowCount);

        if (anchor is null)
        {
            Browser.True(HasAppendedRowVisible);
        }
    }

    [Fact]
    public void EndAnchoredAppend_IssuesExactlyTwoProviderCalls_AdvancingToTail()
    {
        Navigate($"{ServerPathBase}/virtualize-append?comparer=true");
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

    private static string[] GetRenderedItems(IWebElement container)
    {
        var itemElements = container.FindElements(By.CssSelector(".virtualize-item"));
        return itemElements.Select(element => element.Text).ToArray();
    }

    private static void ScrollTopToEnd(IWebDriver browser, IWebElement elem)
    {
        var js = (IJavaScriptExecutor)browser;
        js.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", elem);
    }

    private int GetPlaceholderCellCount() => Browser.FindElements(By.CssSelector(PlaceholderCells)).Count;

    private int GetDataRowCount() => Browser.FindElements(By.CssSelector(DataRows)).Count;

    private int GetRowCount()
        => int.Parse(Browser.FindElement(By.Id("repro-rowcount")).Text, CultureInfo.InvariantCulture);

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
        => int.Parse(el.GetAttribute(name), CultureInfo.InvariantCulture);

    private int GetIntValue(string id)
    {
        var el = Browser.FindElement(By.Id(id));
        return int.Parse(el.GetAttribute("value"), CultureInfo.InvariantCulture);
    }

    // Appended rows have indices >= the seed count; a visible one proves the tail actually loaded.
    private bool HasAppendedRowVisible()
    {
        foreach (var row in Browser.FindElements(By.CssSelector(DataRows)))
        {
            var match = Regex.Match(row.Text, @"Log entry (\d+)");
            if (match.Success && int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) >= InitialItemCount)
            {
                return true;
            }
        }

        return false;
    }

    private void ClickById(string id)
    {
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("document.getElementById(arguments[0]).click();", id);
    }
}
