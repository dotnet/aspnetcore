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
public partial class MessagingTests : BrowserTest
{
    [TestMethod]
    public async Task Messaging_ReceivesJsonFromNativePortsAndOtherTabsInBothRenderModes()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        var sender = await context.NewPageAsync();
        await sender.GotoAsync(server.TestUrl);
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);

        foreach (var path in new[] { "/messaging-server", "/messaging-wasm" })
        {
            await StartReceivingAsync(page, $"{server.TestUrl}{path}");
            await page.Locator("#roundtrip-messaging").ClickAsync();
            await Expect(page.Locator("#receive-port")).ToHaveAttributeAsync("data-run", "1");
            await Expect(page.Locator("#receive-port")).ToHaveTextAsync("Port: \"ping\"");
            var origin = new Uri(server.TestUrl).GetLeftPart(UriPartial.Authority);
            string[] payloads =
            [
                """{"hasOwnProperty":"value","__jsObjectId":"data","__dotNetObject":7,"__proto__":{"value":1},"nested":[null,true,42]}""",
                """["array",{"value":1}]""",
                "\"text\"",
                "\"\"",
                "42",
                "true",
                "null",
            ];
            for (var index = 0; index < payloads.Length; index++)
            {
                var json = payloads[index];
                await page.EvaluateAsync("json => window.messagingNativePort.postMessage(JSON.parse(json))", json);
                await Expect(page.Locator("#receive-port")).ToHaveAttributeAsync("data-run", (index + 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
                await Expect(page.Locator("#receive-port")).ToHaveTextAsync($"Port: {json}");
                await sender.EvaluateAsync("""
                    json => {
                        const channel = new BroadcastChannel('components-platform-receiving');
                        channel.postMessage(JSON.parse(json));
                        channel.close();
                    }
                    """, json);
                await Expect(page.Locator("#receive-broadcast")).ToHaveAttributeAsync("data-run", (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                await Expect(page.Locator("#receive-broadcast")).ToHaveTextAsync($"Broadcast: {json} | {origin}");
            }

            await page.Locator("#cancel-messaging").ClickAsync();
            await Expect(page.Locator("#receive-messaging-status")).ToHaveTextAsync("Canceled.");
            await page.WaitForFunctionAsync("() => window.messagingTargets[2].listeners.get('message').size === 0");
            // Canceling the broadcast registration does not remove either port's listeners.
            await page.Locator("#roundtrip-messaging").ClickAsync();
            await Expect(page.Locator("#receive-port")).ToHaveAttributeAsync("data-run", (payloads.Length + 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Expect(page.Locator("#receive-port")).ToHaveTextAsync("Port: \"ping\"");
            await page.Locator("#dispose-messaging").ClickAsync();
            await Expect(page.Locator("#receive-messaging-status")).ToHaveTextAsync("Targets disposed.");
            Assert.IsTrue(await page.EvaluateAsync<bool>("() => window.messagingTargets.every(state => state.closed && [...state.listeners.values()].every(listeners => listeners.size === 0))"));
        }

        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task Messaging_RejectsNonJsonNativeMessagesAndKeepsListening()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);

        foreach (var path in new[] { "/messaging-server", "/messaging-wasm" })
        {
            await StartReceivingAsync(page, $"{server.TestUrl}{path}");
            await page.EvaluateAsync("""
                () => {
                    const cycle = {};
                    cycle.self = cycle;
                    const invalid = [
                        new Date(0), new Map([[1, 2]]), new Uint8Array([1]), undefined,
                        NaN, Infinity, -Infinity, 1n, { nested: [new Date(0)] },
                        { missing: undefined }, new Array(1), cycle
                    ];
                    const broadcast = new window.messagingNativeBroadcast('components-platform-receiving');
                    for (const message of invalid) {
                        window.messagingNativePort.postMessage(message);
                        broadcast.postMessage(message);
                    }
                    window.messagingNativePort.postMessage('after-invalid');
                    broadcast.postMessage('after-invalid');
                    broadcast.close();
                }
                """);

            // Each sender's valid sentinel follows every rejected message in native delivery order.
            await Expect(page.Locator("#receive-port")).ToHaveTextAsync("Port: \"after-invalid\"");
            await Expect(page.Locator("#receive-broadcast")).ToContainTextAsync("Broadcast: \"after-invalid\"");
            await Expect(page.Locator("#receive-port")).ToHaveAttributeAsync("data-run", "1");
            await Expect(page.Locator("#receive-broadcast")).ToHaveAttributeAsync("data-run", "1");
            await Expect(page.Locator("#receive-messageerror")).ToHaveAttributeAsync("data-run", "0");
            await page.Locator("#dispose-messaging").ClickAsync();
            await Expect(page.Locator("#receive-messaging-status")).ToHaveTextAsync("Targets disposed.");
        }

        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    private static async Task StartReceivingAsync(IPage page, string url)
    {
        await page.GotoAsync(url);
        await page.WaitForInteractiveAsync("#listen-messaging");
        await page.EvaluateAsync("""
            () => {
                window.messagingTargets = [];
                const track = target => {
                    const state = { listeners: new Map(), closed: false };
                    window.messagingTargets.push(state);
                    const add = target.addEventListener;
                    const remove = target.removeEventListener;
                    const close = target.close;
                    target.addEventListener = function(type, listener, options) {
                        add.call(this, type, listener, options);
                        if (!state.listeners.has(type)) {
                            state.listeners.set(type, new Set());
                        }
                        state.listeners.get(type).add(listener);
                    };
                    target.removeEventListener = function(type, listener, options) {
                        remove.call(this, type, listener, options);
                        state.listeners.get(type)?.delete(listener);
                    };
                    target.close = function() {
                        close.call(this);
                        state.closed = true;
                    };
                };
                const NativeMessageChannel = window.MessageChannel;
                window.MessageChannel = class extends NativeMessageChannel {
                    constructor() {
                        super();
                        track(this.port1);
                        track(this.port2);
                        window.messagingNativePort = this.port2;
                    }
                };
                const NativeBroadcastChannel = window.BroadcastChannel;
                window.messagingNativeBroadcast = NativeBroadcastChannel;
                window.BroadcastChannel = class extends NativeBroadcastChannel {
                    constructor(name) {
                        super(name);
                        track(this);
                    }
                };
            }
            """);
        await page.Locator("#listen-messaging").ClickAsync();
        await Expect(page.Locator("#receive-messaging-status")).ToHaveTextAsync("Listening.");
        Assert.AreEqual(5, await page.EvaluateAsync<int>("() => window.messagingTargets.reduce((count, state) => count + [...state.listeners.values()].reduce((sum, listeners) => sum + listeners.size, 0), 0)"));
    }

    [TestMethod]
    public async Task Messaging_PostsJsonToNativeReceiversInBothRenderModes()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);

        foreach (var path in new[] { "/messaging-server", "/messaging-wasm" })
        {
            await page.GotoAsync($"{server.TestUrl}{path}");
            await page.WaitForInteractiveAsync("#send-port");
            await page.EvaluateAsync("""
                () => {
                    window.messagingReceived = { ports: [], broadcasts: [] };
                    const NativeMessageChannel = window.MessageChannel;
                    window.MessageChannel = class extends NativeMessageChannel {
                        constructor() {
                            super();
                            this.port2.onmessage = event => window.messagingReceived.ports.push(event.data);
                        }
                    };
                    window.messagingReceiver = new BroadcastChannel('components-platform-messaging');
                    window.messagingReceiver.onmessage = event => window.messagingReceived.broadcasts.push(event.data);
                }
                """);

            (string Format, string Input, string Expected)[] payloads =
            [
                ("json", """{"hasOwnProperty":"value","__jsObjectId":"data","__dotNetObject":7,"nested":[null,true,42]}""", """{"hasOwnProperty":"value","__jsObjectId":"data","__dotNetObject":7,"nested":[null,true,42]}"""),
                ("json", """["array",{"value":1}]""", """["array",{"value":1}]"""),
                ("json", "\"text\"", "\"text\""),
                ("json", "42", "42"),
                ("json", "true", "true"),
                ("json", "null", "null"),
                ("string", """{"__jsObjectId":1}""", "\"{\\\"__jsObjectId\\\":1}\""),
                ("string", "", "\"\""),
                ("typed", """{"__jsObjectId":"data","version":2}""", """{"__jsObjectId":"data","version":2}"""),
                ("collection", """[{"__jsObjectId":"data","version":3}]""", """[{"__jsObjectId":"data","version":3}]"""),
                ("nullable", "null", "null"),
                ("nullable", "42", "42"),
            ];
            for (var index = 0; index < payloads.Length; index++)
            {
                var (format, input, json) = payloads[index];
                await page.Locator("#message-format").SelectOptionAsync(format);
                await page.Locator("#message-json").FillAsync(input);
                await page.Locator("#send-port").ClickAsync();
                await Expect(page.Locator("#messaging-result")).ToHaveTextAsync("Port message posted.");
                await page.Locator("#send-broadcast").ClickAsync();
                await Expect(page.Locator("#messaging-result")).ToHaveTextAsync("Broadcast posted: components-platform-messaging");
                await page.WaitForFunctionAsync("""
                    ({ index, json }) => {
                        const expected = JSON.parse(json);
                        return ['ports', 'broadcasts'].every(kind =>
                            window.messagingReceived[kind].length === index + 1 &&
                            JSON.stringify(window.messagingReceived[kind][index]) === JSON.stringify(expected));
                    }
                    """, new { index, json });
            }

            await page.Locator("#close-messaging").ClickAsync();
            await Expect(page.Locator("#messaging-result")).ToHaveTextAsync("Channels closed.");
            await page.EvaluateAsync("() => window.messagingReceiver.close()");
        }

        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }
}
