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
public partial class MessageEventTests : BrowserTest
{
    private static readonly string[] MessagePaths = ["/message-event-server", "/message-event-wasm"];

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
    public async Task MessageEvent_DeliversAnyJsonValueAsJsonElement()
    {
        foreach (var path in MessagePaths)
        {
            await StartListeningAsync(path);

            await PostAndExpectAsync("\"text\"", "\"text\"");
            await PostAndExpectAsync("42", "42");
            await PostAndExpectAsync("true", "true");
            await PostAndExpectAsync("null", "null");
            await PostAndExpectAsync(
                """{ name: "日本語", list: [1, 2.5, "x", null, true], nested: { a: { b: [{ c: 1 }] } } }""",
                """{"name":"日本語","list":[1,2.5,"x",null,true],"nested":{"a":{"b":[{"c":1}]}}}""");
        }
    }

    [TestMethod]
    public async Task MessageEvent_DeliversMarkersForValuesThatAreNotJson()
    {
        foreach (var path in MessagePaths)
        {
            await StartListeningAsync(path);

            await PostAndExpectAsync(
                "{ when: new Date(0), map: new Map([[1, 2]]), list: [new Date(0)], text: 'kept' }",
                """{"when":{"__unsupported":"Date"},"map":{"__unsupported":"Map"},"list":[{"__unsupported":"Date"}],"text":"kept"}""");
        }
    }

    private async Task StartListeningAsync(string path)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync("#listen-message");
        await _page.Locator("#listen-message").ClickAsync();
        await Expect(_page.Locator("#message-result")).ToHaveTextAsync("Listening.");
    }

    // The message event reaches the page asynchronously, so the test waits for the run counter to go up by one.
    private async Task PostAndExpectAsync(string messageExpression, string expectedJson)
    {
        var run = int.Parse(await _page.Locator("#message-result").GetAttributeAsync("data-run") ?? "0", CultureInfo.InvariantCulture);

        await _page.EvaluateAsync($"window.postMessage({messageExpression}, '*')");

        await Expect(_page.Locator("#message-result")).ToHaveAttributeAsync("data-run", (run + 1).ToString(CultureInfo.InvariantCulture));
        await Expect(_page.Locator("#message-result")).ToHaveTextAsync(expectedJson);
    }
}
