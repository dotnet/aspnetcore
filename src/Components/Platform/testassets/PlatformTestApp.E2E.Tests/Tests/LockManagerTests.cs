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
public partial class LockManagerTests : BrowserTest
{
    private static readonly string[] LockManagerPaths = ["/locks-server", "/locks-wasm"];

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
    public async Task QueryAsync_ReadsLocksHeldAndRequestedByThePageInBothRenderModes()
    {
        foreach (var path in LockManagerPaths)
        {
            await NavigateAsync(path, "#query-locks");

            await RunAsync("#query-locks", "Query: held [], pending []");

            // The page's own JavaScript holds an exclusive lock, so its shared request waits behind it.
            var clientId = await _page.EvaluateAsync<string>("""
                async () => {
                    const held = new Promise(resolve => window.releaseE2ELock = resolve);
                    window.e2eExclusive = navigator.locks.request('e2e-lock', () => held);
                    window.e2eShared = navigator.locks.request('e2e-lock', { mode: 'shared' }, () => {});
                    return (await navigator.locks.query()).held[0].clientId;
                }
                """);
            Assert.IsFalse(string.IsNullOrEmpty(clientId));

            await RunAsync("#query-locks", $"Query: held [e2e-lock exclusive {clientId}], pending [e2e-lock shared {clientId}]");

            await _page.EvaluateAsync("""
                async () => {
                    window.releaseE2ELock();
                    await window.e2eExclusive;
                    await window.e2eShared;
                }
                """);

            await RunAsync("#query-locks", "Query: held [], pending []");
        }
    }

    [TestMethod]
    public async Task RequestAsync_HoldsTheLockWhileTheCallbackRunsInBothRenderModes()
    {
        foreach (var path in LockManagerPaths)
        {
            await NavigateAsync(path, "#request-lock");

            await RunAsync("#request-lock", "Request: held e2e-dotnet-lock exclusive");
            Assert.IsFalse(await IsAvailableAsync("e2e-dotnet-lock"), "The browser should hold the lock while the .NET callback runs.");

            await RunAsync("#release-lock", "Request: released");
            Assert.IsTrue(await IsAvailableAsync("e2e-dotnet-lock"), "The browser should release the lock when the .NET callback completes.");

            await RunAsync("#request-lock-throwing", "Request with throwing callback: InvalidOperationException: The work failed.");
            Assert.IsTrue(await IsAvailableAsync("e2e-dotnet-lock"), "The browser should release the lock when the .NET callback throws.");
        }
    }

    [TestMethod]
    public async Task RequestAsync_SendsEachOptionInBothRenderModes()
    {
        foreach (var path in LockManagerPaths)
        {
            await NavigateAsync(path, "#request-lock-shared");

            // The page's own JavaScript holds a shared lock and an exclusive lock until the end of the step.
            await _page.EvaluateAsync("""
                () => {
                    const held = new Promise(resolve => window.releaseE2ELocks = resolve);
                    window.e2eShared = navigator.locks.request('e2e-shared', { mode: 'shared' }, () => held);
                    window.e2eBusy = navigator.locks.request('e2e-busy', () => held);
                    return navigator.locks.query();
                }
                """);

            // Without mode, the request would wait behind the shared lock, and the step would time out.
            await RunAsync("#request-lock-shared", "Request shared: granted e2e-shared shared");
            await RunAsync("#request-lock-if-available", "Request if available: not granted");
            await RunAsync("#request-lock-steal", "Request with steal: granted e2e-busy exclusive");
            Assert.AreEqual("AbortError", await _page.EvaluateAsync<string>("() => window.e2eBusy.then(() => 'resolved', error => error.name)"));

            await _page.EvaluateAsync("""
                async () => {
                    window.releaseE2ELocks();
                    await window.e2eShared;
                }
                """);
        }
    }

    // Requests the lock from JavaScript only if no one holds it, and releases it at once.
    private Task<bool> IsAvailableAsync(string name)
        => _page.EvaluateAsync<bool>("name => navigator.locks.request(name, { ifAvailable: true }, grantedLock => grantedLock !== null)", name);

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task RunAsync(string buttonSelector, string expectedResult)
    {
        var result = _page.Locator("#locks-result");
        // Waiting for the next run keeps a step from passing on the text of the previous step.
        var nextRun = int.Parse(await result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await Expect(result).ToHaveTextAsync(expectedResult);
    }
}
