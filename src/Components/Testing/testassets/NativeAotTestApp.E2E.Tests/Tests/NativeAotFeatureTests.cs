// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeAotTestApp.Components;
using NativeAotTestApp.E2E.Tests.Fixtures;

namespace NativeAotTestApp.E2E.Tests.Tests;

[UITest]
public partial class NativeAotFeatureTests : BrowserTest
{
    private readonly ConcurrentQueue<string> _browserErrors = new();
    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(
            TestRoot.Servers,
            options =>
            {
                options.EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "true";
                options.ReadinessTimeoutMs = 120_000;
            });

        var context = await NewContext(new BrowserNewContextOptions());
        _page = await context.NewPageAsync();
        _page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                _browserErrors.Enqueue($"Console: {message.Text}");
            }
        };
        _page.PageError += (_, message) => _browserErrors.Enqueue($"Page: {message}");
        _page.WebSocket += (_, socket) =>
            socket.SocketError += (_, message) => _browserErrors.Enqueue($"WebSocket: {message}");
    }

    [TestMethod]
    public async Task InteractiveShell_RendersAndHandlesDynamicUi()
    {
        await GotoInteractiveAsync("/", "#menu-action");

        await Expect(_page.Locator("#menu-panel h2")).ToHaveTextAsync("Resource menu");
        await _page.Locator("#menu-action").ClickAsync();
        await Expect(_page.Locator("#shell-result")).ToHaveTextAsync("menu:resources");

        await _page.Locator("#dialog-tab").ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await Expect(_page.Locator("#dialog-theme")).ToHaveTextAsync("native-theme");
        await Expect(_page.Locator("#dialog-greeting")).ToHaveTextAsync("injected-greeting");
        await _page.Locator("#dialog-value").FillAsync("restart");
        await _page.Locator("#dialog-submit").ClickAsync();
        await Expect(_page.Locator("#shell-result")).ToHaveTextAsync("dialog:restart");

        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task Forms_BindValidateAndSubmitDashboardInputMatrix()
    {
        await GotoInteractiveAsync("/forms", "form");

        await _page.Locator("#form-submit").ClickAsync();
        await Expect(_page.Locator(".validation-message")).ToContainTextAsync("required");
        await Expect(_page.Locator("#form-result")).ToHaveTextAsync("(unsubmitted)");

        await _page.Locator("#form-name").FillAsync("worker");
        await _page.Locator("#form-count").FillAsync("12");
        await _page.Locator("#form-enabled").CheckAsync();
        await _page.Locator("#form-mode").SelectOptionAsync("Detailed");
        await _page.Locator("#form-optional-mode").SelectOptionAsync("Compact");
        await _page.Locator("#form-nested").FillAsync("west");
        await _page.Locator("#form-list").FillAsync("resource-1");
        await _page.Locator("#form-submit").ClickAsync();

        await Expect(_page.Locator("#form-result"))
            .ToHaveTextAsync("worker|12|True|Detailed|Compact|west|resource-1");
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task JsInterop_ResolversAndCustomEventsRoundTrip()
    {
        await GotoInteractiveAsync("/interop", "#interop-run");

        await _page.Locator("#interop-run").ClickAsync();

        await Expect(_page.Locator("#interop-poco")).ToHaveTextAsync("counter:21|42");
        await Expect(_page.Locator("#interop-dotnet")).ToHaveTextAsync("echo:dashboard");
        await Expect(_page.Locator("#interop-element")).ToHaveTextAsync("interop-target");
        await Expect(_page.Locator("#interop-wire")).ToHaveTextAsync("""{"someValue":"first"}""");
        await Expect(_page.Locator("#interop-event")).ToHaveTextAsync("horizontal|0.4");
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task ProtectedStorage_RestoresAcrossReload()
    {
        await GotoInteractiveAsync("/storage", "#storage-save");
        var firstInstance = await _page.Locator("#storage-instance").TextContentAsync();

        await _page.Locator("#storage-save").ClickAsync();
        await Expect(_page.Locator("#storage-value")).ToHaveTextAsync("saved");
        await Expect(_page.Locator("#storage-converter"))
            .ToHaveTextAsync(new System.Text.RegularExpressions.Regex("[1-9][0-9]*\\|0"));

        await _page.ReloadAsync();
        await WaitForNativeCircuitAsync();

        await Expect(_page.Locator("#storage-value")).ToHaveTextAsync("ada:36");
        await Expect(_page.Locator("#storage-instance")).Not.ToHaveTextAsync(firstInstance!);
        await Expect(_page.Locator("#storage-converter"))
            .ToHaveTextAsync(new System.Text.RegularExpressions.Regex("[1-9][0-9]*\\|[1-9][0-9]*"));
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task PersistentState_RestoresAcrossPrerender()
    {
        await _page.GotoAsync(_server.AppUrl + "/persistence");
        var token = await _page.Locator("#persistence-token").TextContentAsync();
        Assert.IsFalse(string.IsNullOrEmpty(token));

        await WaitForNativeCircuitAsync();

        await Expect(_page.Locator("#persistence-phase")).ToHaveTextAsync("circuit (restored)");
        await Expect(_page.Locator("#persistence-token")).ToHaveTextAsync(token!);
        await Expect(_page.Locator("#persistence-serializer"))
            .ToHaveTextAsync(new System.Text.RegularExpressions.Regex("[1-9][0-9]*\\|[1-9][0-9]*"));
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task Virtualize_ScrollsAndLoadsRows()
    {
        await GotoInteractiveAsync("/virtualization");

        await Expect(_page.GetByText("row-0", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(_page.GetByText("row-400", new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(_page.Locator("#expression-created")).ToHaveTextAsync("1|1");
        await Expect(_page.Locator("#expression-invoked")).ToHaveTextAsync("2|1");
        await Expect(_page.Locator("#expression-value")).ToHaveTextAsync("10000");

        await _page.Locator("#viewport").EvaluateAsync("element => element.scrollTop = 8000");

        await Expect(_page.GetByText("row-400", new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task InputFile_StreamsAndSubmitsFile()
    {
        await GotoInteractiveAsync("/file-upload", "form");
        const string content = "native-aot-file";

        await _page.Locator("#file-input").SetInputFilesAsync(new FilePayload
        {
            Name = "probe.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes(content),
        });

        await Expect(_page.Locator("#file-read"))
            .ToHaveTextAsync($"probe.txt|{content}|{Encoding.UTF8.GetByteCount(content)}");
        await _page.Locator("#file-submit").ClickAsync();
        await Expect(_page.Locator("#file-result"))
            .ToHaveTextAsync($"submitted:probe.txt:{content}:{Encoding.UTF8.GetByteCount(content)}");
        await AssertHealthyAsync();
    }

    [TestMethod]
    public async Task EnhancedNavigation_QueryAndHistoryRemainInteractive()
    {
        await GotoInteractiveAsync("/history?count=7&enabled=true", "#history-increment");

        await Expect(_page.Locator("#history-count")).ToHaveTextAsync("7");
        await Expect(_page.Locator("#history-enabled")).ToHaveTextAsync("True");
        await _page.Locator("#history-increment").ClickAsync();
        await Expect(_page.Locator("#history-clicks")).ToHaveTextAsync("1");

        var nextNavigation = _page.WaitForEnhancedNavigationAsync();
        await _page.Locator("#history-next").ClickAsync();
        await nextNavigation;
        await WaitForNativeCircuitAsync();
        await _page.WaitForInteractiveAsync("#history-increment");
        await Expect(_page.Locator("#history-count")).ToHaveTextAsync("11");
        await Expect(_page.Locator("#history-enabled")).ToHaveTextAsync("False");
        await _page.Locator("#history-increment").ClickAsync();
        await Expect(_page.Locator("#history-clicks")).ToHaveTextAsync("2");

        var backNavigation = _page.WaitForEnhancedNavigationAsync();
        await _page.GoBackAsync();
        await backNavigation;
        await WaitForNativeCircuitAsync();
        await _page.WaitForInteractiveAsync("#history-increment");
        await Expect(_page.Locator("#history-count")).ToHaveTextAsync("7");
        await _page.Locator("#history-increment").ClickAsync();
        await Expect(_page.Locator("#history-clicks")).ToHaveTextAsync("3");

        var forwardNavigation = _page.WaitForEnhancedNavigationAsync();
        await _page.GoForwardAsync();
        await forwardNavigation;
        await WaitForNativeCircuitAsync();
        await _page.WaitForInteractiveAsync("#history-increment");
        await Expect(_page.Locator("#history-count")).ToHaveTextAsync("11");
        await _page.Locator("#history-increment").ClickAsync();
        await Expect(_page.Locator("#history-clicks")).ToHaveTextAsync("4");
        await AssertHealthyAsync();
    }

    private async Task GotoInteractiveAsync(string path, string? interactiveSelector = null)
    {
        await _page.GotoAsync(_server.AppUrl + path);
        await WaitForNativeCircuitAsync();

        if (interactiveSelector is not null)
        {
            await _page.WaitForInteractiveAsync(interactiveSelector);
        }
    }

    private async Task WaitForNativeCircuitAsync()
    {
        await _page.WaitForBlazorAsync();
        await Expect(_page.Locator("#runtime-mode"))
            .ToHaveAttributeAsync("data-dynamic-code-supported", "false");
        await Expect(_page.Locator("#runtime-mode"))
            .ToHaveAttributeAsync("data-circuit-attached", "true");
    }

    private async Task AssertHealthyAsync()
    {
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        Assert.AreEqual(
            0,
            _browserErrors.Count,
            string.Join(Environment.NewLine, _browserErrors));
    }
}
