// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class AbortLinkTests : BrowserTest
{
    private static readonly string[] AbortLinkPaths = ["/abortlink-server", "/abortlink-wasm"];

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        _context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await _context.NewPageAsync();
    }

    [TestMethod]
    public async Task AbortLink_LeavesTheRequestAloneUntilTheTokenIsCanceled()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#fetch-not-canceled");

            await RunAsync("#fetch-not-canceled", "Not canceled: ok");
            await AssertServerSawAsync("completed");

            await RunAsync("#fetch-without-token", "Without token: ok");
            await AssertServerSawAsync("completed");
        }
    }

    [TestMethod]
    public async Task AbortLink_AbortsTheRequestInTheBrowserWhenTheTokenIsCanceled()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#cancel-before-headers");

            await RunAsync("#cancel-before-headers", "Cancel before headers: Canceled (JSException)");
            await AssertServerSawAsync("aborted");

            // The signal stays bound to the response, so canceling also stops reading the body.
            await RunAsync("#cancel-during-body", "Cancel during body: Canceled (JSException)");
            await AssertServerSawAsync("aborted");
        }
    }

    [TestMethod]
    public async Task AbortLink_ThrowsBeforeSendingTheRequestForACanceledToken()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#cancel-before-start");

            await RunAsync("#cancel-before-start", "Cancel before start: Canceled (no inner exception)");
            await AssertServerSawAsync("unknown");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task RunAsync(string buttonSelector, string expectedResult)
    {
        var result = _page.Locator("#abortlink-result");
        // Waiting for the next run keeps a step from passing on the text of the previous step.
        var nextRun = int.Parse(await result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await Expect(result).ToHaveTextAsync(expectedResult);
    }

    // The server notices a closed connection a little after the browser rejects, so poll for its record.
    private async Task AssertServerSawAsync(string expectedState)
    {
        var requestId = await _page.Locator("#abortlink-result").GetAttributeAsync("data-request-id");
        var deadline = DateTime.UtcNow.AddSeconds(10);
        string state;
        do
        {
            state = await _page.EvaluateAsync<string>(
                "id => fetch('/abortlink/log/' + id).then(response => response.text())",
                requestId);
            if (state == expectedState)
            {
                return;
            }

            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        Assert.AreEqual(expectedState, state, $"The server state of request {requestId}.");
    }
}
