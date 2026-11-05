// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ModelContextProtocol.Client;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Renders an original MCP invocation using an application-configured content host.</summary>
/// <typeparam name="THost">The application component that renders the iframe and configures MCP Apps hosting.</typeparam>
public sealed class McpBlock<THost> : ComponentBase, IAsyncDisposable where THost : McpContentHost
{
    private readonly CancellationTokenSource _lifetime = new();
    private FunctionInvocationContentBlock? _renderedBlock;
    private McpClient? _renderedClient;
    private IDisposable? _subscription;
    private IJSObjectReference? _module;
    private Task? _import;
    private bool _disposed;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<McpBlock<THost>> Logger { get; set; } = default!;

    /// <summary>Gets or sets the originating function invocation block.</summary>
    [Parameter, EditorRequired] public FunctionInvocationContentBlock Block { get; set; } = default!;

    /// <summary>Gets or sets the application-owned client for this app's resource reads and operations.</summary>
    [Parameter, EditorRequired] public McpClient Client { get; set; } = default!;

    /// <summary>Gets or sets the already-discovered SDK tool snapshot, including model-facing aliases.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<McpClientTool> Tools { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Block);
        ArgumentNullException.ThrowIfNull(Client);
        ArgumentNullException.ThrowIfNull(Tools);
        if (_renderedBlock is not null &&
            (_renderedBlock.Id != Block.Id ||
                _renderedBlock.ToolName != Block.ToolName ||
                !ReferenceEquals(_renderedClient, Client)))
        {
            throw new InvalidOperationException("Use @key with the originating call ID when replacing an MCP app.");
        }
        if (!ReferenceEquals(_renderedBlock, Block))
        {
            _subscription?.Dispose();
            _renderedBlock = Block;
            _renderedClient = Client;
            _subscription = Block.OnChanged(() => _ = InvokeAsync(StateHasChanged));
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_module is null || _disposed)
        {
            return;
        }
        builder.OpenComponent<CascadingValue<McpClient>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<McpClient>.Value), Client);
        builder.AddAttribute(2, nameof(CascadingValue<McpClient>.IsFixed), true);
        builder.AddAttribute(3, nameof(CascadingValue<McpClient>.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<THost>(0);
            content.AddAttribute(1, nameof(McpContentHost.Block), Block);
            content.AddAttribute(2, nameof(McpContentHost.Tools), Tools);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed)
        {
            return _import = ImportAsync();
        }
        return Task.CompletedTask;
    }

    private async Task ImportAsync()
    {
        try
        {
            var module = await JS.InvokeAsync<IJSObjectReference>("import", _lifetime.Token, McpContentHost.ModulePath);
            if (_disposed)
            {
                await module.DisposeAsync();
                return;
            }
            _module = module;
            StateHasChanged();
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during module import.");
            throw;
        }
        catch (JSException error)
        {
            Logger.LogWarning(error, "The MCP app host module failed to import.");
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _subscription?.Dispose();
        _lifetime.Cancel();
        try
        {
            if (_import is not null)
            {
                try
                {
                    await _import;
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                }
            }
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during module cleanup.");
        }
        finally
        {
            _lifetime.Dispose();
        }
    }

}
