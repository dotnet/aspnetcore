// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using McpAppsHost.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace McpAppsHost.E2E.Tests.Tests;

[UITest]
public partial class McpAppHostingTests : BrowserTest
{
    private readonly HttpClient _http = new();
    private readonly List<string> _errors = [];
    private ServerInstance? _server;
    private ServerInstance? _otherServer;
    private string _sandboxOrigin = null!;
    private ServerInstance? _host;
    private IPage _page = null!;
    private IFrameLocator _content = null!;

    [TestMethod]
    public async Task SandboxBootstrapUsesApplicationPathBase()
    {
        await OpenAsync(pathBase: "/chat");

        await Expect(_content.Locator("#message")).ToHaveTextAsync("Hello from a standard app.");
        await SaveAsync("Saved beneath the application path base");
        await _page.Locator(".mcp-theme").ClickAsync();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        using var rootAsset = await _http.GetAsync(_sandboxOrigin + "/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js");
        Assert.AreEqual(HttpStatusCode.NotFound, rootAsset.StatusCode);
        using var asset = await _http.GetAsync(_sandboxOrigin + "/chat/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js");
        Assert.AreEqual(HttpStatusCode.OK, asset.StatusCode);
        var document = await _http.GetStringAsync(_sandboxOrigin + "/chat/default-sandbox");
        StringAssert.Contains(document, "/chat/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js");
        Assert.AreEqual(1, (await ServerStateAsync()).GetProperty("initialCalls").GetInt32());
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LargeDocumentsOriginalResultsAndAppRequestsAreDeliveredIntact()
    {
        await OpenAsync(largePayloads: true);

        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-payload-length", "2097153");
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-payload-matches", "true");
        await _content.Locator("#echo-payload").ClickAsync();

        await Expect(_content.Locator("#echo-length")).ToHaveTextAsync("2097153");
        await Expect(_content.Locator("#echo-matches")).ToHaveTextAsync("true");
        await _page.Locator("#rerender").ClickAsync();
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        Assert.AreEqual(1, (await ServerStateAsync()).GetProperty("initialCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task MoreThanThirtyTwoAppRequestsRemainIndependentAndCancellable()
    {
        await OpenAsync();

        await _content.Locator("#many-slow").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 33);
        await Expect(_content.Locator("#status")).ToHaveTextAsync("Waiting");
        await _page.Locator(".mcp-close").ClickAsync();

        await Expect(_page.Locator("iframe")).ToHaveCountAsync(0);
        await WaitForServerStateAsync("activeCalls", 0);
        await WaitForServerStateAsync("cancelledCalls", 33);
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task InitializationDoesNotExpireAfterThirtySeconds()
    {
        await OpenAsync(prompt: "Open a note and wait until I save", waitForOriginalResult: false, holdResource: true);
        await WaitForServerStateAsync("resourceReads", 1);

        await Task.Delay(TimeSpan.FromSeconds(31));
        using var released = await _http.PostAsync($"{_server!.AppUrl}/release-resource", null);
        released.EnsureSuccessStatusCode();

        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");
        await SaveAsync("Saved after delayed initialization");
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task SandboxEndpointsRenderStandaloneDocumentsOnlyOnTheirConfiguredOrigin()
    {
        await OpenAsync();

        foreach (var path in new[] { "/mcp-sandbox", "/default-sandbox" })
        {
            using var response = await _http.GetAsync(_sandboxOrigin + path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Headers.CacheControl!.NoStore);
            Assert.AreEqual("text/html", response.Content.Headers.ContentType!.MediaType);
            var html = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(html.StartsWith("<!doctype html>", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(html.Contains("id=\"mcp-app\"", StringComparison.Ordinal));
            Assert.IsTrue(html.Contains("mcp-sandbox.js", StringComparison.Ordinal));
            Assert.IsFalse(html.Contains("blazor.web.js", StringComparison.Ordinal));
            Assert.IsFalse(html.Contains("<h1>Standard MCP Apps host", StringComparison.Ordinal));
            using var wrongOrigin = await _http.GetAsync(_host!.AppUrl + path);
            Assert.AreEqual(HttpStatusCode.NotFound, wrongOrigin.StatusCode);
        }
        using var chatOnSandbox = await _http.GetAsync(_sandboxOrigin);
        Assert.AreEqual(HttpStatusCode.NotFound, chatOnSandbox.StatusCode);
        var sandbox = _page.Locator(".mcp-host > iframe").ContentFrame;
        await Expect(sandbox.Locator("iframe")).ToHaveCountAsync(1);
        await Expect(sandbox.Locator("#mcp-app")).ToHaveAttributeAsync("title", "MCP app document");
        await Expect(sandbox.Locator("#mcp-app")).ToHaveAttributeAsync("allow", "clipboard-write 'none'");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task EarlySandboxReadinessAndLateResultPreserveBothFrameIdentities()
    {
        await OpenAsync(prompt: "Open a note and wait until I save", waitForOriginalResult: false,
            holdResource: true, initialize: page => page.AddInitScriptAsync("""
                window.__sandboxReady = false;
                window.addEventListener("message", event => {
                    if (event.source === document.querySelector(".mcp-host > iframe")?.contentWindow &&
                        event.data?.method === "ui/notifications/sandbox-proxy-ready") {
                        window.__sandboxReady = true;
                    }
                });
                """));
        await WaitForServerStateAsync("resourceReads", 1);
        await _page.WaitForFunctionAsync("window.__sandboxReady === true");
        await _page.EvaluateAsync("window.__outer = document.querySelector('.mcp-host > iframe')");
        using var released = await _http.PostAsync($"{_server!.AppUrl}/release-resource", null);
        released.EnsureSuccessStatusCode();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");
        var sandbox = _page.Locator(".mcp-host > iframe").ContentFrame;
        await sandbox.Locator("body").EvaluateAsync("() => window.__inner = document.getElementById('mcp-app')");

        await SaveAsync("Result delivered after initialization");
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        await _page.Locator("#rerender").ClickAsync();
        await _page.Locator(".mcp-theme").ClickAsync();

        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        Assert.IsTrue(await _page.EvaluateAsync<bool>("window.__outer === document.querySelector('.mcp-host > iframe')"));
        Assert.IsTrue(await sandbox.Locator("body").EvaluateAsync<bool>("() => window.__inner === document.getElementById('mcp-app')"));
        Assert.AreEqual(600, await _page.Locator(".mcp-host > iframe").EvaluateAsync<int>("frame => frame.clientHeight"));
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        Assert.AreEqual(1, (await ServerStateAsync()).GetProperty("initialCalls").GetInt32());
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task SandboxReadinessAfterBridgeConnectionInitializesWithoutRenavigation()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await OpenAsync(waitForOriginalResult: false, initialize: async page =>
        {
            await page.AddInitScriptAsync("""
                window.__unattachedReady = false;
                window.__wrongOriginReady = false;
                addEventListener("message", event => {
                    if (event.source === document.getElementById("unattached-sandbox")?.contentWindow &&
                        event.data?.method === "ui/notifications/sandbox-proxy-ready") {
                        window.__unattachedReady = true;
                    }
                    if (event.source === document.querySelector(".mcp-host > iframe")?.contentWindow &&
                        event.data?.method === "ui/notifications/sandbox-proxy-ready" &&
                        event.origin !== new URL(document.querySelector(".mcp-host > iframe").src).origin) {
                        window.__wrongOriginReady = true;
                    }
                });
                """);
            await page.RouteAsync("**/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js", async route =>
            {
                if (route.Request.Frame.Url == _sandboxOrigin + "/mcp-sandbox")
                {
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(25));
                }
                await route.ContinueAsync();
            });
            await page.RouteAsync("**/mcp-sandbox", async route =>
            {
                var response = await route.FetchAsync();
                var html = await response.TextAsync();
                var probe = $$$"""
                    <script>
                    window.__bridgeConnected = false;
                    const timer = setInterval(() => parent.postMessage(
                        {jsonrpc:"2.0",id:"connection-probe",method:"ping",params:{}},
                        {{{JsonSerializer.Serialize(TestRoot.Servers.ProxyUrl)}}}), 25);
                    addEventListener("message", event => {
                        if (event.origin === {{{JsonSerializer.Serialize(TestRoot.Servers.ProxyUrl)}}} &&
                            event.data?.id === "connection-probe" && "result" in event.data) {
                            clearInterval(timer);
                            window.__bridgeConnected = true;
                        }
                    });
                    </script>
                    """;
                await route.FulfillAsync(new() { Response = response, Body = html.Replace("</head>", probe + "</head>", StringComparison.Ordinal) });
            });
            await page.RouteAsync("**/wrong-origin-readiness", route => route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = $$$"""
                    <script>parent.postMessage(
                        {jsonrpc:"2.0",method:"ui/notifications/sandbox-proxy-ready",params:{}},
                        {{{JsonSerializer.Serialize(TestRoot.Servers.ProxyUrl)}}});
                    </script>
                    """
            }));
        });
        var outer = _page.Locator(".mcp-host > iframe");
        await Expect(outer.ContentFrame.Locator("body")).ToBeVisibleAsync();
        await _page.Frames.Single(frame => frame.Url == _sandboxOrigin + "/mcp-sandbox")
            .WaitForFunctionAsync("window.__bridgeConnected === true");
        await _page.EvaluateAsync("""
            source => {
                const frame = document.createElement("iframe");
                frame.id = "unattached-sandbox";
                frame.hidden = true;
                frame.src = source;
                document.body.append(frame);
            }
            """, _sandboxOrigin + "/default-sandbox");
        await _page.WaitForFunctionAsync("window.__unattachedReady === true");
        await _page.EvaluateAsync("document.getElementById('unattached-sandbox').remove()");
        var target = _page.Frames.Single(frame => frame.Url == _sandboxOrigin + "/mcp-sandbox");
        await target.GotoAsync(_host!.AppUrl + "/wrong-origin-readiness");
        await _page.WaitForFunctionAsync("window.__wrongOriginReady === true");
        await target.GotoAsync(_sandboxOrigin + "/mcp-sandbox", new() { WaitUntil = WaitUntilState.Commit });
        var source = await outer.GetAttributeAsync("src");

        release.SetResult();

        await Expect(_content.Locator("#status")).ToHaveTextAsync("Ready");
        await Expect(outer).ToHaveAttributeAsync("src", source!);
        await Expect(outer.ContentFrame.Locator("iframe")).ToHaveCountAsync(1);
        AssertNoBrowserErrors();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ClosingInitializationCannotResurrectViewOrBreakLaterModelCall(bool holdResource)
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var modules = 0;
        await OpenAsync(waitForOriginalResult: false, holdResource: holdResource, initialize: page =>
            page.RouteAsync("**/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js", async route =>
            {
                if (!holdResource && Interlocked.Increment(ref modules) == 1)
                {
                    requested.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(25));
                }
                await route.ContinueAsync();
            }));
        await WaitForServerStateAsync("resourceReads", 1);
        if (!holdResource)
        {
            await requested.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        await Expect(_page.Locator(".mcp-host > iframe")).ToHaveCountAsync(1);
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();

        await _page.Locator(".mcp-close").ClickAsync();

        await Expect(_page.Locator(".mcp-host")).ToHaveCountAsync(0);
        await Expect(_page.Locator("iframe")).ToHaveCountAsync(0);
        release.TrySetResult();
        using var released = await _http.PostAsync($"{_server!.AppUrl}/release-resource", null);
        released.EnsureSuccessStatusCode();
        await _page.Locator("textarea.sc-ai-input__textarea").FillAsync("Open another note");
        await _page.Locator("button.sc-ai-input__send").ClickAsync();

        await Expect(_content.Locator("#status")).ToHaveTextAsync("Ready");
        await Expect(_page.Locator(".mcp-host")).ToHaveCountAsync(1);
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        Assert.AreEqual(2, (await ServerStateAsync()).GetProperty("initialCalls").GetInt32());
        await SaveAsync("App opened after an initializing view closed");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task DefaultSandboxDerivesPermissionsWithoutChangingFixedHostAttributes()
    {
        await OpenAsync(initialize: page => page.RouteAsync("**/mcp-sandbox", async route =>
        {
            var html = await _http.GetStringAsync(_sandboxOrigin + "/default-sandbox");
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = html });
        }));
        var outer = _page.Locator(".mcp-host > iframe");
        var inner = outer.ContentFrame.Locator("#mcp-app");

        await Expect(inner).ToHaveAttributeAsync("allow", "clipboard-write");
        await Expect(outer).ToHaveAttributeAsync("allow", "clipboard-write 'none'");
        var csp = await _content.Locator("meta[http-equiv='Content-Security-Policy']").GetAttributeAsync("content");
        Assert.IsNotNull(csp);
        StringAssert.Contains(csp, "base-uri 'self'");
        StringAssert.Contains(csp, "frame-src 'none'");
        await SaveAsync("Saved through default sandbox");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task OriginalSdkResultAndDirectAppActionDoNotReplayOrInvokeModel()
    {
        await OpenAsync();

        var frame = _page.Locator(".mcp-host > iframe");
        await Expect(frame).ToHaveAttributeAsync("title", "Application-configured MCP host");
        await Expect(frame).ToHaveAttributeAsync("sandbox", "allow-scripts allow-same-origin");
        await Expect(frame).ToHaveAttributeAsync("allow", "clipboard-write 'none'");
        await Expect(_content.Locator("#message")).ToHaveTextAsync("Hello from a standard app.");
        await Expect(_content.Locator("#metadata")).ToHaveTextAsync("Not for the model.");
        await Expect(_content.Locator("#arguments")).ToContainTextAsync("Original note");
        await SaveAsync("A direct app action");

        await _page.Locator(".mcp-theme").ClickAsync();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        await _content.Locator("#load-asset").ClickAsync();
        await Expect(_content.Locator("#allowed-asset")).ToHaveAttributeAsync("data-loaded", "true");
        await _content.Locator("#connect-socket").ClickAsync();
        await Expect(_content.Locator("#socket-result")).ToHaveTextAsync("Network round trip");
        await _page.Locator("#rerender").ClickAsync();
        await Expect(_page.Locator("#render-count")).ToHaveTextAsync("1");
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");

        var state = await ServerStateAsync();
        Assert.AreEqual(1, state.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("savedCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("assetRequests").GetInt32());
        Assert.AreEqual(1, state.GetProperty("socketRequests").GetInt32());
        await AssertModelUnchangedAsync();
        await Expect(_page.Locator(".sc-ai-message--user")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".sc-ai-approval")).ToHaveCountAsync(0);

        await _page.Locator(".mcp-close").ClickAsync();
        await Expect(_page.Locator("iframe")).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task MultipleModelToolCallsHaveIndependentThemeAndClose()
    {
        await OpenAsync(prompt: "Open two notes");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
        var initial = await ServerStateAsync();
        Assert.AreEqual(2, initial.GetProperty("initialCalls").GetInt32());
        var second = _page.Locator("iframe[title='Application-configured MCP host']")
            .Nth(1).ContentFrame.FrameLocator("iframe[title='MCP app document']");
        await Expect(second.Locator("#status")).ToHaveTextAsync("Ready");
        await Expect(_page.Locator(".mcp-host")).ToHaveCountAsync(2);
        await Expect(_content.Locator("#arguments")).ToContainTextAsync("Original note");
        await Expect(second.Locator("#arguments")).ToContainTextAsync("Second note");
        await Expect(_content.Locator("#title")).ToHaveTextAsync("Original note");
        await Expect(second.Locator("#title")).ToHaveTextAsync("Second note");
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");
        await Expect(second.Locator("#theme")).ToHaveTextAsync("light");

        await _page.Locator(".mcp-host").Nth(0).Locator(".mcp-theme").ClickAsync();

        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        await Expect(second.Locator("#theme")).ToHaveTextAsync("light");
        await _page.Locator("#rerender").ClickAsync();
        await Expect(_page.Locator("#render-count")).ToHaveTextAsync("1");
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        await Expect(second.Locator("#theme")).ToHaveTextAsync("light");

        await _content.Locator("#slow").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 1);
        await second.Locator("#slow").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 2);
        await _page.Locator(".mcp-host").Nth(0).Locator(".mcp-close").ClickAsync();

        await Expect(_page.Locator(".mcp-host")).ToHaveCountAsync(1);
        await Expect(_page.Locator("iframe")).ToHaveCountAsync(1);
        await WaitForServerStateAsync("activeCalls", 1);
        await WaitForServerStateAsync("cancelledCalls", 1);
        await Expect(_content.Locator("#status")).ToHaveTextAsync("Waiting");
        await _content.Locator("#cancel").ClickAsync();
        await Expect(_content.Locator("#status")).ToHaveTextAsync("Cancelled");
        await WaitForServerStateAsync("activeCalls", 0);
        await WaitForServerStateAsync("cancelledCalls", 2);
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");
        await SaveAsync("Remaining app");
        await _page.Locator(".mcp-theme").ClickAsync();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        var state = await ServerStateAsync();
        Assert.AreEqual(2, state.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("savedCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task OrdinaryFunctionInvocationsKeepTheirDefaultRendering()
    {
        await OpenAsync(prompt: "Open a note and run the regular tool");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();

        await Expect(_page.Locator(".mcp-host")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".sc-ai-unknown-block")).ToHaveCountAsync(0);
        var state = await ServerStateAsync();
        Assert.AreEqual(1, state.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("modelOnlyCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AliasedAppToolUsesItsBlockScopedClientAndSnapshot()
    {
        await OpenAsync(withOtherConnection: true, prompt: "Open the other note");

        await SaveAsync("Saved through the aliased app");

        var first = await ServerStateAsync();
        var second = await ServerStateAsync(_otherServer);
        Assert.AreEqual(0, first.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(0, first.GetProperty("savedCalls").GetInt32());
        Assert.AreEqual(1, second.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(1, second.GetProperty("savedCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AppCanSaveBeforeOriginalModelToolCompletes()
    {
        await OpenAsync(prompt: "Open a note and wait until I save", waitForOriginalResult: false);
        await WaitForServerStateAsync("initialCalls", 1);
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");
        await Expect(_content.Locator("#arguments")).ToContainTextAsync("Original note");
        await Expect(_content.Locator("#metadata")).ToHaveTextAsync("");
        var before = await _http.GetFromJsonAsync<JsonElement>($"{_host!.AppUrl}/test-state");
        Assert.AreEqual(1, before.GetProperty("modelRequests").GetInt32());

        await SaveAsync("Saved while original call was pending");

        await Expect(_content.Locator("#status")).ToHaveTextAsync("Ready");
        await Expect(_content.Locator("#metadata")).ToHaveTextAsync("Not for the model.");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
        var state = await ServerStateAsync();
        Assert.AreEqual(1, state.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("savedCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    [DataRow("Open text content", "text")]
    [DataRow("Open text and image content", "text,image")]
    public async Task ConvertedSdkContentIsDeliveredToTheApp(string prompt, string contentTypes)
    {
        await OpenAsync(prompt: prompt);

        await Expect(_content.Locator("#message")).ToHaveTextAsync("Converted SDK content");
        await Expect(_content.Locator("#metadata")).ToHaveTextAsync("Preserved");
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-content-types", contentTypes);
        await _page.Locator("#rerender").ClickAsync();
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        var state = await ServerStateAsync();
        Assert.AreEqual(1, state.GetProperty("initialCalls").GetInt32());
        var model = await _http.GetFromJsonAsync<JsonElement>($"{_host!.AppUrl}/test-state");
        Assert.AreEqual(2, model.GetProperty("modelRequests").GetInt32());
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task McpErrorResultIsPreservedForTheApp()
    {
        await OpenAsync(prompt: "Open a failing note");

        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-is-error", "true");
        await Expect(_content.Locator("#metadata")).ToHaveTextAsync("Not for the model.");
        await Expect(_content.Locator("#message")).ToHaveTextAsync("The note operation failed.");
        await _page.Locator("#rerender").ClickAsync();
        await Expect(_content.Locator("#status")).ToHaveAttributeAsync("data-result-count", "1");
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task OriginalToolTransportFailureNotifiesTheApp()
    {
        await OpenAsync(prompt: "Open a note and wait until I save", waitForOriginalResult: false);
        await WaitForServerStateAsync("activeInitialCalls", 1);
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");

        await _server!.DisposeAsync();
        _server = null;

        await Expect(_content.Locator("#error")).ToHaveTextAsync("Initial call cancelled");
        await _page.Locator(".mcp-theme").ClickAsync();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task CancellingModelTurnWithoutAResultNotifiesTheApp()
    {
        await OpenAsync(prompt: "Open a note and wait until I save", waitForOriginalResult: false);
        await WaitForServerStateAsync("activeInitialCalls", 1);
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("light");

        await _page.Locator(".stop-response").ClickAsync();

        await WaitForServerStateAsync("activeInitialCalls", 0);
        await WaitForServerStateAsync("cancelledInitialCalls", 1);
        await Expect(_content.Locator("#error")).ToHaveTextAsync("Initial call cancelled");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AppActionRunsWhileModelResponseIsStreaming()
    {
        await OpenAsync(holdResponse: true);
        await Expect(_page.Locator(".sc-ai-typing")).ToHaveCountAsync(1);

        await SaveAsync("While streaming");

        await Expect(_page.Locator(".sc-ai-typing")).ToHaveCountAsync(1);
        await AssertModelUnchangedAsync();
        var state = await ServerStateAsync();
        Assert.AreEqual(1, state.GetProperty("savedCalls").GetInt32());

        await _page.Locator("#release").ClickAsync();
        await Expect(_page.Locator(".sc-ai-typing")).ToHaveCountAsync(0);
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AppCannotCallModelOnlyOrOtherConnectionTools()
    {
        await OpenAsync(withOtherConnection: true);

        foreach (var (button, toolName) in new[]
        {
            ("#model-only", "model_only"),
            ("#unknown", "unknown_tool"),
            ("#cross-server", "other_show_note"),
        })
        {
            await _content.Locator(button).ClickAsync();
            await Expect(_content.Locator("#error")).ToContainTextAsync("not available on the owning connection");
            await Expect(_content.Locator("#error")).ToContainTextAsync(toolName);
        }

        await SaveAsync("On the owning connection");
        var state = await ServerStateAsync();
        var other = await ServerStateAsync(_otherServer);
        Assert.AreEqual(0, state.GetProperty("modelOnlyCalls").GetInt32());
        Assert.AreEqual(1, state.GetProperty("savedCalls").GetInt32());
        Assert.AreEqual(0, other.GetProperty("initialCalls").GetInt32());
        Assert.AreEqual(0, other.GetProperty("savedCalls").GetInt32());
        Assert.AreEqual(0, other.GetProperty("modelOnlyCalls").GetInt32());
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AppCancellationReachesServerAndLeavesViewUsable()
    {
        await OpenAsync();
        await _content.Locator("#slow").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 1);

        await _content.Locator("#cancel").ClickAsync();

        await Expect(_content.Locator("#status")).ToHaveTextAsync("Cancelled");
        await WaitForServerStateAsync("activeCalls", 0);
        await WaitForServerStateAsync("cancelledCalls", 1);
        await SaveAsync("After cancellation");
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task ServerTransportFailureIsReturnedToAppWithoutBreakingHostPipe()
    {
        await OpenAsync();
        await _content.Locator("#transport-failure").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 1);

        await _server!.DisposeAsync();
        _server = null;

        await Expect(_content.Locator("#error")).ToContainTextAsync("wait_for_cancellation:");
        await Expect(_content.Locator("#message")).ToHaveTextAsync("Hello from a standard app.");
        await _page.Locator(".mcp-theme").ClickAsync();
        await Expect(_content.Locator("#theme")).ToHaveTextAsync("dark");
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task ClosingViewCancelsPendingCallWithoutResumingModel()
    {
        await OpenAsync();
        await _content.Locator("#slow").ClickAsync();
        await WaitForServerStateAsync("activeCalls", 1);

        await _page.Locator(".mcp-close").ClickAsync();

        await Expect(_page.Locator("iframe")).ToHaveCountAsync(0);
        await WaitForServerStateAsync("activeCalls", 0);
        await WaitForServerStateAsync("cancelledCalls", 1);
        await AssertModelUnchangedAsync();
        AssertNoBrowserErrors();
    }

    private async Task OpenAsync(bool holdResponse = false, bool withOtherConnection = false,
        string prompt = "Open the note", bool waitForOriginalResult = true, bool holdResource = false,
        Func<IPage, Task>? initialize = null, bool largePayloads = false, string? pathBase = null)
    {
        var instance = Guid.NewGuid().ToString("N");
        _server = await StartServerAsync<global::McpAppsServer.ServerAssembly>(TestRoot.Servers,
            options =>
            {
                options.EnvironmentVariables["TEST_INSTANCE"] = instance;
                options.EnvironmentVariables["HOLD_RESOURCE"] = holdResource.ToString();
                options.EnvironmentVariables["LARGE_PAYLOADS"] = largePayloads.ToString();
            });
        if (withOtherConnection)
        {
            _otherServer = await StartServerAsync<global::McpAppsServer.ServerAssembly>(TestRoot.Servers,
                options => options.EnvironmentVariables["TEST_INSTANCE"] = instance + "-other");
        }
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            _sandboxOrigin = $"http://localhost:{((IPEndPoint)listener.LocalEndpoint).Port}";
        }
        _host = await StartServerAsync<global::McpAppsHost.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["HOST_ORIGIN"] = TestRoot.Servers.ProxyUrl;
            options.EnvironmentVariables["SANDBOX_ORIGIN"] = _sandboxOrigin;
            options.EnvironmentVariables["MCP_SERVER_URL"] = $"{_server.AppUrl}/mcp";
            options.EnvironmentVariables["HOLD_RESPONSE"] = holdResponse.ToString();
            options.EnvironmentVariables["LARGE_PAYLOADS"] = largePayloads.ToString();
            if (pathBase is not null)
            {
                options.EnvironmentVariables["PATH_BASE"] = pathBase;
            }
            options.EnvironmentVariables["TEST_INSTANCE"] = instance;
            if (_otherServer is not null)
            {
                options.EnvironmentVariables["OTHER_MCP_SERVER_URL"] = $"{_otherServer.AppUrl}/mcp";
            }
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_host));
        _page = await context.NewPageAsync();
        if (initialize is not null)
        {
            await initialize(_page);
        }
        _page.PageError += (_, error) => _errors.Add(error);
        _page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                _errors.Add(message.Text);
            }
        };
        await _page.GotoAsync(_host.TestUrl.TrimEnd('/') + pathBase + "/");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
        await Expect(_page.Locator("#interactive-status")).ToHaveTextAsync("ready");
        await _page.Locator("textarea.sc-ai-input__textarea").FillAsync(prompt);
        await _page.Locator("button.sc-ai-input__send").ClickAsync();
        _content = _page.Locator("iframe[title='Application-configured MCP host']")
            .Nth(0).ContentFrame.FrameLocator("iframe[title='MCP app document']");
        if (waitForOriginalResult)
        {
            await Expect(_content.Locator("#status")).ToHaveTextAsync("Ready");
        }
    }

    private async Task SaveAsync(string text)
    {
        await _content.Locator("#draft").FillAsync(text);
        await _content.Locator("#save").ClickAsync();
        await Expect(_content.Locator("#message")).ToHaveTextAsync($"Saved: {text}");
    }

    private Task<JsonElement> ServerStateAsync(ServerInstance? server = null)
    {
        return _http.GetFromJsonAsync<JsonElement>($"{(server ?? _server)!.AppUrl}/state");
    }

    private async Task WaitForServerStateAsync(string property, int expected)
    {
        var elapsed = Stopwatch.StartNew();
        int actual;
        do
        {
            actual = (await ServerStateAsync()).GetProperty(property).GetInt32();
            if (actual == expected)
            {
                return;
            }
            await Task.Delay(50);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(15));

        Assert.AreEqual(expected, actual, $"Server property {property} did not reach the expected state.");
    }

    private async Task AssertModelUnchangedAsync()
    {
        var state = await _http.GetFromJsonAsync<JsonElement>($"{_host!.AppUrl}/test-state");
        Assert.AreEqual(2, state.GetProperty("modelRequests").GetInt32());
        Assert.IsTrue(state.GetProperty("receivedViewMetadata").GetBoolean());
        var tools = state.GetProperty("modelToolNames").EnumerateArray().Select(tool => tool.GetString()).ToArray();
        CollectionAssert.Contains(tools, "show_note");
        CollectionAssert.DoesNotContain(tools, "save_note");
        CollectionAssert.DoesNotContain(tools, "wait_for_cancellation");
    }

    private void AssertNoBrowserErrors()
    {
        Assert.AreEqual(0, _errors.Count, string.Join(Environment.NewLine, _errors));
    }

    protected override async Task CleanupCoreAsync()
    {
        await base.CleanupCoreAsync();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
        if (_otherServer is not null)
        {
            await _otherServer.DisposeAsync();
        }
        _http.Dispose();
    }
}
