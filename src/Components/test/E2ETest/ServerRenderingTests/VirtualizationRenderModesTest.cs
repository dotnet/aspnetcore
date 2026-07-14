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
    [InlineData(false)]
    [InlineData(true)]
    public void AsyncItemsProvider_DoesNotFlashPlaceholderRows_OnAnchoredEdgeGrowth(bool comparer)
    {
        var parts = new List<string> { "gate=true" };
        if (comparer)
        {
            parts.Add("comparer=true");
        }
        Navigate($"{ServerPathBase}/virtualize-append?{string.Join("&", parts)}");

        Browser.Exists(By.Id("interactive-ready"));
        var initialCount = GetReportedItemCount();

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);

        // Arm the gate so any fetch that advances past the loaded window blocks, then grow the list.
        ClickById("close-gate");
        ClickById("append-btn");

        // The core assertion: while the advancing fetch is pending, no placeholder flashes.
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.True(() => GetDataRowCount() > 0);

        var batch = GetInputValue("batch-input");

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        Browser.Equal(0, GetPlaceholderCellCount);
        Browser.Equal(initialCount + batch, GetReportedItemCount);

        Browser.True(() => HasAppendedRowVisible(initialCount));
    }

    [Fact]
    public void StartAnchoredPrepend_MovesViewportToNewHead_WithoutPlaceholderFlash()
    {
        Navigate($"{ServerPathBase}/virtualize-append?anchor=beginning&comparer=true&gate=true");
        Browser.Exists(By.Id("interactive-ready"));

        var initialCount = GetReportedItemCount();
        var batch = GetInputValue("batch-input");

        ClickById("open-gate");
        Browser.True(() => GetDataRowCount() > 0);
        // Precondition: at the top, the head row is the very first seeded item (entry 0).
        Browser.Equal(0, GetTopVisibleRowEntryNumber);
        Browser.Equal(0, GetVisiblePlaceholderCellCount);

        // Arm the gate so the prepend's head fetch blocks, then prepend.
        ClickById("close-gate");
        ClickById("prepend-btn");

        // The core assertion: while the head fetch is pending, no placeholder flashes in the viewport.
        Browser.Equal(0, GetVisiblePlaceholderCellCount);

        ClickById("open-gate");
        Browser.Equal(initialCount + batch, GetReportedItemCount);

        // The viewport followed up to the newly prepended head (entry index >= seed count),
        // stayed pinned to the top, and never flashed a placeholder.
        Browser.True(() => GetTopVisibleRowEntryNumber() >= initialCount);
        Browser.Equal(0L, GetScrollTop);
        Browser.Equal(0, GetVisiblePlaceholderCellCount);
    }

    [Fact]
    public void EndAnchoredAppend_IssuesExactlyTwoProviderCalls_AdvancingToTail()
    {
        Navigate($"{ServerPathBase}/virtualize-append?comparer=true");
        Browser.Exists(By.Id("interactive-ready"));
        var initialCount = GetReportedItemCount();
        var batch = GetInputValue("batch-input");

        // Initial load fetches the top window (start=0) then, once End-anchored, the tail (start>0).
        Browser.True(() => GetProviderCalls().Any(c => c.Total == initialCount && c.Start == 0));
        Browser.True(() => GetProviderCalls().Any(c => c.Total == initialCount && c.Start > 0));
        var tailStart = GetProviderCalls().Where(c => c.Total == initialCount).Max(c => c.Start);

        ClickById("append-btn");

        // Exactly two calls for the append — the fix relocates the tail fetch, it doesn't add one.
        Browser.Equal(2, () => GetProviderCalls().Count(c => c.Total == initialCount + batch));
        var appendCalls = GetProviderCalls().Where(c => c.Total == initialCount + batch).ToList();

        Assert.Contains(appendCalls, c => c.Start == tailStart);
        Assert.Contains(appendCalls, c => c.Start == tailStart + batch);
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

    private int GetPlaceholderCellCount() => Browser.FindElements(By.CssSelector("#repro-scroll-container td.grid-cell-placeholder")).Count;

    private int GetVisiblePlaceholderCellCount()
        => Convert.ToInt32(((IJavaScriptExecutor)Browser).ExecuteScript(
            @"var c = document.getElementById('repro-scroll-container');
              var view = c.getBoundingClientRect();
              var cells = c.querySelectorAll('td.grid-cell-placeholder');
              var n = 0;
              for (var i = 0; i < cells.length; i++) {
                  var r = cells[i].getBoundingClientRect();
                  if (r.height > 0 && r.bottom > view.top && r.top < view.bottom) { n++; }
              }
              return n;"), CultureInfo.InvariantCulture);

    private long GetScrollTop()
        => Convert.ToInt64(((IJavaScriptExecutor)Browser).ExecuteScript(
            "return document.getElementById('repro-scroll-container').scrollTop;"), CultureInfo.InvariantCulture);

    private int GetTopVisibleRowEntryNumber()
    {
        var value = ((IJavaScriptExecutor)Browser).ExecuteScript(
            @"var c = document.getElementById('repro-scroll-container');
              var top = c.getBoundingClientRect().top;
              var rows = c.querySelectorAll('tr.repro-row:not(.placeholder-row)');
              for (var i = 0; i < rows.length; i++) {
                  if (rows[i].getBoundingClientRect().bottom > top + 1) {
                      var m = rows[i].innerText.match(/Log entry (\d+)/);
                      return m ? parseInt(m[1], 10) : -1;
                  }
              }
              return -1;");
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private IReadOnlyCollection<IWebElement> GetDataRows()
        => Browser.FindElements(By.CssSelector("#repro-scroll-container tr.repro-row:not(.placeholder-row)"));

    private int GetDataRowCount() => GetDataRows().Count;

    private int GetReportedItemCount()
        => int.Parse(Browser.FindElement(By.Id("repro-rowcount")).Text, CultureInfo.InvariantCulture);

    private readonly record struct ProviderCall(int Start, int Count, int Total);

    // GetLog drains Chrome's buffer on each read, so accumulate calls across polls.
    private readonly List<ProviderCall> _observedProviderCalls = new();

    private static readonly Regex ProviderCallRegex =
        new(@"VIRTUALIZE_PCALL start=(\d+) count=(\d+) total=(\d+)", RegexOptions.Compiled);

    private List<ProviderCall> GetProviderCalls()
    {
        foreach (var entry in Browser.Manage().Logs.GetLog(LogType.Browser))
        {
            var match = ProviderCallRegex.Match(entry.Message);
            if (match.Success)
            {
                _observedProviderCalls.Add(new ProviderCall(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)));
            }
        }

        return _observedProviderCalls;
    }

    private int GetInputValue(string id)
    {
        var el = Browser.FindElement(By.Id(id));
        return int.Parse(el.GetAttribute("value"), CultureInfo.InvariantCulture);
    }

    // Appended rows have indices >= the seed count; a visible one proves the tail actually loaded.
    private bool HasAppendedRowVisible(int seedCount)
    {
        foreach (var row in GetDataRows())
        {
            var match = Regex.Match(row.Text, @"Log entry (\d+)");
            if (match.Success && int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) >= seedCount)
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
