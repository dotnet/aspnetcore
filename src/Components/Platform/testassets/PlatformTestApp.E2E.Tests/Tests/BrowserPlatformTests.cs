// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class BrowserPlatformTests : BrowserTest
{
    private static readonly string[] StoragePaths = ["/storage-server", "/storage-wasm"];
    private static readonly string[] UrlPaths = ["/url-server", "/url-wasm"];
    private static readonly string[] FetchPaths = ["/fetch-server", "/fetch-wasm"];

    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task WebStorage_WorksInBothRenderModes()
    {
        foreach (var path in StoragePaths)
        {
            await NavigateAsync(path, "#save-storage");

            var value = path == "/storage-server" ? "server-value" : "wasm-value";
            await _page.Locator("#storage-value").FillAsync(value);
            await _page.Locator("#save-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync($"Saved: {value}");

            await _page.Locator("#storage-value").FillAsync(string.Empty);
            await _page.Locator("#load-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync($"Loaded: {value}");

            await _page.Locator("#remove-storage").ClickAsync();
            await _page.Locator("#load-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync("Value is missing.");
        }
    }

    [TestMethod]
    public async Task Url_WorksInBothRenderModes()
    {
        foreach (var path in UrlPaths)
        {
            await NavigateAsync(path, "#create-url");

            await _page.Locator("#create-url").ClickAsync();
            await Expect(_page.Locator("#url-result")).ToHaveTextAsync(
                "https://example.test/products?page=2&tag=one#featured");

            await _page.Locator("#use-disposed-url").ClickAsync();
            await Expect(_page.Locator("#url-result")).ToHaveTextAsync("Disposed URL rejected.");
        }
    }

    [TestMethod]
    public async Task Fetch_WorksInBothRenderModes()
    {
        foreach (var path in FetchPaths)
        {
            await NavigateAsync(path, "#fetch-echo");

            await _page.Locator("#fetch-echo").ClickAsync();
            await Expect(_page.Locator("#fetch-result")).ToHaveTextAsync(
                "200:browser:request-body");

            await _page.Locator("#fetch-missing").ClickAsync();
            await Expect(_page.Locator("#fetch-result")).ToHaveTextAsync("404:False");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }
}
