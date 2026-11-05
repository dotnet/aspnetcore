// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using ModelContextProtocol.Client;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpResultDeliveryTests
{
    [Theory]
    [InlineData("before")]
    [InlineData("during")]
    [InlineData("after")]
    public async Task ResultIsDeliveredOnceAcrossInitializationAndRerenders(string timing)
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var browser = new Browser();
        await using var host = new DeliveryHost(client, (await client.ListToolsAsync()).ToArray(), browser);
        if (timing == "before")
        {
            host.Block.Result = Result();
        }

        var initializing = host.RenderAsync(firstRender: true);
        await browser.Attaching.Task;
        if (timing == "during")
        {
            host.Block.Result = Result();
        }
        Assert.Empty(browser.Results);
        browser.Attached.SetResult();
        await initializing;
        if (timing == "after")
        {
            Assert.Empty(browser.Results);
            host.Block.Result = Result();
        }

        host.UpdateParameters();
        await host.RenderAsync(firstRender: false);
        host.UpdateParameters();
        await host.RenderAsync(firstRender: false);

        var delivered = Assert.Single(browser.Results);
        Assert.Equal("Metadata", delivered.GetProperty("_meta").GetProperty("app").GetString());
        Assert.Equal(0, server.ToolCalls);
        Assert.Equal(1, server.ToolLists);
        Assert.Empty(browser.Cancellations);
    }

    [Fact]
    public async Task FunctionExceptionIsNotDeliveredAsASuccess()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var browser = new Browser();
        await using var host = new DeliveryHost(client, (await client.ListToolsAsync()).ToArray(), browser);
        host.Block.Result = new("call", "Generic model-facing failure")
        {
            Exception = new InvalidOperationException("The original invocation failed.")
        };
        var initializing = host.RenderAsync(firstRender: true);
        await browser.Attaching.Task;
        browser.Attached.SetResult();
        await initializing;
        await host.RenderAsync(firstRender: false);

        Assert.Empty(browser.Results);
        Assert.Equal("The original invocation failed.", Assert.Single(browser.Cancellations));
    }

    [Fact]
    public async Task DisposedViewDoesNotDeliverALateResultOrDisposeClient()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var browser = new Browser();
        var host = new DeliveryHost(client, (await client.ListToolsAsync()).ToArray(), browser);
        var initializing = host.RenderAsync(firstRender: true);
        await browser.Attaching.Task;
        browser.Attached.SetResult();
        await initializing;

        await host.DisposeAsync();
        host.Block.Result = Result();
        await host.RenderAsync(firstRender: false);
        await client.PingAsync();

        Assert.Empty(browser.Results);
        Assert.False(server.Disposed);
    }

    private static FunctionResultContent Result() => new("call",
        JsonSerializer.SerializeToElement(new
        {
            content = new[] { new { type = "text", text = "Result" } },
            _meta = new { app = "Metadata" }
        }));

    private sealed class DeliveryHost : McpContentHost
    {
        public DeliveryHost(McpClient client, IReadOnlyList<McpClientTool> tools, Browser browser)
        {
            Client = client;
            Tools = tools;
            Block = new() { Call = new("call", "open", new Dictionary<string, object?>()) };
            JS = browser;
            Frame = new("frame");
            OnParametersSet();
        }

        public Task RenderAsync(bool firstRender) => OnAfterRenderAsync(firstRender);
        public void UpdateParameters() => OnParametersSet();
    }

    private sealed class Browser : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource Attaching { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Attached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<JsonElement> Results { get; } = [];
        public List<string> Cancellations { get; } = [];

#pragma warning disable BL0016 // This in-memory test double does not invoke JavaScript.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeCoreAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeCoreAsync<TValue>(identifier, cancellationToken, args);
#pragma warning restore BL0016

        private async ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import")
            {
                return (TValue)(object)this;
            }
            if (identifier == "attach")
            {
                Attaching.TrySetResult();
                await Attached.Task.WaitAsync(cancellationToken);
                return (TValue)(object)this;
            }
            if (identifier == "sendToolResult")
            {
                Results.Add(((JsonElement)args![0]!).Clone());
            }
            if (identifier == "sendToolCancelled")
            {
                Cancellations.Add((string)args![0]!);
            }
            return default!;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
