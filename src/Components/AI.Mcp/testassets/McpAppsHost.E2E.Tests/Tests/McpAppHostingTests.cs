// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
    private ServerInstance? _proxy;
    private ServerInstance? _host;
    private IPage _page = null!;
    private IFrameLocator _content = null!;

    [TestMethod]
    public async Task OriginalResultAndDirectAppActionPreserveModelProjectionAndDoNotReplay()
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

    private async Task OpenAsync(bool holdResponse = false, bool withOtherConnection = false, string prompt = "Open the note")
    {
        var instance = Guid.NewGuid().ToString("N");
        _server = await StartServerAsync<global::McpAppsServer.ServerAssembly>(TestRoot.Servers,
            options => options.EnvironmentVariables["TEST_INSTANCE"] = instance);
        if (withOtherConnection)
        {
            _otherServer = await StartServerAsync<global::McpAppsServer.ServerAssembly>(TestRoot.Servers,
                options => options.EnvironmentVariables["TEST_INSTANCE"] = instance + "-other");
        }
        _proxy = await StartServerAsync<global::McpAppsHost.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["HOST_ORIGIN"] = TestRoot.Servers.ProxyUrl;
            options.EnvironmentVariables["SANDBOX_ONLY"] = "true";
            options.EnvironmentVariables["TEST_INSTANCE"] = instance;
        });
        _host = await StartServerAsync<global::McpAppsHost.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["HOST_ORIGIN"] = TestRoot.Servers.ProxyUrl;
            options.EnvironmentVariables["PROXY_URL"] = $"{_proxy.AppUrl}/proxy";
            options.EnvironmentVariables["MCP_SERVER_URL"] = $"{_server.AppUrl}/mcp";
            options.EnvironmentVariables["HOLD_RESPONSE"] = holdResponse.ToString();
            options.EnvironmentVariables["TEST_INSTANCE"] = instance;
            if (_otherServer is not null)
            {
                options.EnvironmentVariables["OTHER_MCP_SERVER_URL"] = $"{_otherServer.AppUrl}/mcp";
            }
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_host));
        _page = await context.NewPageAsync();
        _page.PageError += (_, error) => _errors.Add(error);
        _page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                _errors.Add(message.Text);
            }
        };
        await _page.GotoAsync(_host.TestUrl);
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
        await Expect(_page.Locator("#interactive-status")).ToHaveTextAsync("ready");
        await _page.Locator("textarea.sc-ai-input__textarea").FillAsync(prompt);
        await _page.Locator("button.sc-ai-input__send").ClickAsync();
        _content = _page.Locator("iframe[title='Application-configured MCP host']")
            .Nth(0).ContentFrame.FrameLocator("iframe[title='MCP app document']");
        await Expect(_content.Locator("#status")).ToHaveTextAsync("Ready");
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
        Assert.IsFalse(state.GetProperty("receivedViewMetadata").GetBoolean());
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
        if (_proxy is not null)
        {
            await _proxy.DisposeAsync();
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
