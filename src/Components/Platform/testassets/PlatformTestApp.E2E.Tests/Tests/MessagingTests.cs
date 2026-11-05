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
