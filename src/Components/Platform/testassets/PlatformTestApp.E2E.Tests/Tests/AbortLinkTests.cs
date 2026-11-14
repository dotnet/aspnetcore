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

    private ILocator Result => _page.Locator("#abortlink-result");

    [TestMethod]
    public async Task AbortLink_LeavesTheRequestAloneUntilTheTokenIsCanceled()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#fetch-not-canceled");

            var requestId = await RunAsync("#fetch-not-canceled", "Not canceled: ok");
            await AssertServerStateAsync(requestId, "completed");

            requestId = await RunAsync("#fetch-without-token", "Without token: ok");
            await AssertServerStateAsync(requestId, "completed");
        }
    }

    [TestMethod]
    public async Task AbortLink_AbortsTheRequestInTheBrowserWhenTheTokenIsCanceled()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#start-before-headers");

            // Cancel only once the server holds the request, so the abort cannot stop it before it is sent.
            var nextRun = await GetNextRunAsync();
            var requestId = await StartAsync("#start-before-headers");
            await AssertServerStateAsync(requestId, "started");
            await _page.Locator("#cancel").ClickAsync();
            await ExpectResultAsync(nextRun, "Cancel before headers: Canceled (JSException)");
            await AssertServerStateAsync(requestId, "aborted");

            // The signal stays bound to the response, so canceling also stops reading the body.
            requestId = await RunAsync("#cancel-during-body", "Cancel during body: Canceled (JSException)");
            await AssertServerStateAsync(requestId, "aborted");
        }
    }

    [TestMethod]
    public async Task AbortLink_ThrowsBeforeSendingTheRequestForACanceledToken()
    {
        foreach (var path in AbortLinkPaths)
        {
            await NavigateAsync(path, "#cancel-before-start");

            var requestId = await RunAsync("#cancel-before-start", "Cancel before start: Canceled (no inner exception)");
            await AssertServerStateAsync(requestId, "unknown");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task<string> RunAsync(string buttonSelector, string expectedResult)
    {
        var nextRun = await GetNextRunAsync();
        var requestId = await StartAsync(buttonSelector);
        await ExpectResultAsync(nextRun, expectedResult);

        return requestId;
    }

    // Waiting for the next run keeps a step from passing on the text of the previous step.
    private async Task<int> GetNextRunAsync()
        => int.Parse(await Result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

    // Each run renders a new request ID as soon as it starts.
    private async Task<string> StartAsync(string buttonSelector)
    {
        var previousId = await Result.GetAttributeAsync("data-request-id") ?? "";
        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(Result).Not.ToHaveAttributeAsync("data-request-id", previousId);

        return await Result.GetAttributeAsync("data-request-id") ?? "";
    }

    private async Task ExpectResultAsync(int nextRun, string expectedResult)
    {
        await Expect(Result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await Expect(Result).ToHaveTextAsync(expectedResult);
    }

    // The server answers once the request reaches the state, because it notices an abort a little after the browser.
    private async Task AssertServerStateAsync(string requestId, string expectedState)
    {
        var state = await _page.EvaluateAsync<string>(
            "([id, state]) => fetch('/abortlink/wait/' + id + '/' + encodeURIComponent(state)).then(response => response.text())",
            new[] { requestId, expectedState });

        Assert.AreEqual(expectedState, state, $"The server state of request {requestId}.");
    }
}
