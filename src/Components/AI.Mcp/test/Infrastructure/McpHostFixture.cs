// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ModelContextProtocol.Client;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

internal sealed class McpHostFixture : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly TestRenderer _renderer;
    private Exception? _initializationError;

    public McpHostFixture(McpClient client, IReadOnlyList<McpClientTool> tools,
        FunctionInvocationContentBlock? block = null)
    {
        Browser = new();
        _services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(Browser).BuildServiceProvider();
        _renderer = new(_services);
        Host = _renderer.CreateHost();
        Host.Configure(client, tools, block ?? new()
        {
            Call = new("call", tools[0].Name, new Dictionary<string, object?>())
        });
    }

    public TestHost Host { get; }
    public TestBrowser Browser { get; }

    public async Task InitializeAsync()
    {
        try
        {
            await Host.InitializeAsync();
        }
        catch (Exception error)
        {
            _initializationError = error;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Host.DisposeAsync();
        }
        catch (Exception error) when (ReferenceEquals(error, _initializationError))
        {
            // Initialization failures were observed by the test and are rethrown by host cleanup.
        }
        await _renderer.DisposeAsync();
        await _services.DisposeAsync();
    }

    internal sealed class TestHost : McpContentHost
    {
        public void Configure(McpClient client, IReadOnlyList<McpClientTool> tools, FunctionInvocationContentBlock block)
        {
            Client = client;
            Tools = tools;
            Block = block;
            Frame = new("frame");
        }

        public Task InitializeAsync()
        {
            OnParametersSet();
            return OnAfterRenderAsync(firstRender: true);
        }

        public ValueTask UpdateAsync(JsonElement context) => UpdateHostContextAsync(context);
    }

    internal sealed class TestBrowser : IJSRuntime, IJSObjectReference
    {
        public JsonElement Document { get; private set; }
        public JsonElement Configuration { get; private set; }
        public List<JsonElement> Results { get; } = [];
        public List<JsonElement> Contexts { get; } = [];
        public List<string> Cancellations { get; } = [];
        public Task DisposeCompletion { get; set; } = Task.CompletedTask;

#pragma warning disable BL0016 // This in-memory test double does not invoke JavaScript.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeCore<TValue>(identifier, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeCore<TValue>(identifier, args);
#pragma warning restore BL0016

        private ValueTask<TValue> InvokeCore<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                return ValueTask.FromResult((TValue)(object)this);
            }
            if (identifier == "attach")
            {
                Configuration = ((JsonElement)args![2]!).Clone();
                Document = JsonSerializer.SerializeToElement(args[3], new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return ValueTask.FromResult((TValue)(object)this);
            }
            if (identifier == "sendToolResult")
            {
                Results.Add(((JsonElement)args![0]!).Clone());
            }
            if (identifier == "updateHostContext")
            {
                Contexts.Add(((JsonElement)args![0]!).Clone());
            }
            if (identifier == "sendToolCancelled")
            {
                Cancellations.Add((string)args![0]!);
            }
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => new(DisposeCompletion);
    }

    private sealed class TestRenderer(IServiceProvider services)
        : Renderer(services, services.GetRequiredService<ILoggerFactory>())
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public TestHost CreateHost() => (TestHost)InstantiateComponent(typeof(TestHost));
        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Rendering failed.", exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
}
