// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ModelContextProtocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Provides MCP Apps communication and lifetime management for an application-rendered iframe.</summary>
/// <remarks>Derive a Razor component from this type, render an iframe with <c>@ref="Frame"</c>, and configure <see cref="Options"/>.</remarks>
public abstract class McpContentHost : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _closeLock = new();
    private McpContentDispatcher? _dispatcher;
    private DotNetObjectReference<DispatchBridge>? _reference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _session;
    private McpAppInvocation? _invocation;
    private Task? _initialization;
    private Task? _closing;
    private volatile bool _closed;
    private bool _disposing;
    private bool _rendered;

    /// <summary>Gets the browser interop runtime for additional application-owned functionality.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<McpContentHost> Logger { get; set; } = default!;

    /// <summary>Gets or sets the conversation host that owns the originating invocation.</summary>
    [Parameter, EditorRequired] public McpAppHost Host { get; set; } = default!;

    /// <summary>Gets or sets the retained originating invocation block.</summary>
    [Parameter, EditorRequired] public McpAppContentBlock Block { get; set; } = default!;

    /// <summary>Gets or sets the application-rendered iframe reference.</summary>
    protected ElementReference Frame { get; set; }

    /// <summary>Gets whether this host has closed and should stop rendering its iframe.</summary>
    protected bool IsClosed => _closed;

    /// <summary>Gets the proxy address, browser policy, resource bounds, and operation policies for this view.</summary>
    protected abstract McpContentOptions Options { get; }

    /// <summary>Gets the trusted module that imports and configures the MCP Apps bridge.</summary>
    protected virtual string ModulePath => "./_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-host.js";

    /// <summary>Runs after the MCP app has initialized.</summary>
    protected virtual ValueTask OnReadyAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>Runs after the view has closed.</summary>
    protected virtual ValueTask OnClosedAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Host);
        ArgumentNullException.ThrowIfNull(Block);
        var invocation = Host.FindInvocation(Block)
            ?? throw new InvalidOperationException("The original MCP app invocation is unavailable.");
        if (_invocation is not null && !ReferenceEquals(_invocation, invocation))
        {
            throw new InvalidOperationException("Use @key with the originating call ID when replacing an MCP app.");
        }
        _invocation = invocation;
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _rendered = true;
        if (firstRender && !_closed)
        {
            _initialization = InitializeAsync();
            return _initialization;
        }
        return Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        Options.Validate();
        if (string.IsNullOrEmpty(Frame.Id))
        {
            throw new InvalidOperationException("The MCP content host must render an iframe with @ref=\"Frame\".");
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        deadline.CancelAfter(Options.InitializationTimeout);
        var token = deadline.Token;
        var document = await Host.LoadDocumentAsync(_invocation!, Options, token).AsTask().WaitAsync(token);
        _dispatcher = new(Options, HandleRequestAsync);
        _reference = DotNetObjectReference.Create(new DispatchBridge(_dispatcher));
        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", token,
                ModulePath);
            token.ThrowIfCancellationRequested();
            _session = await _module.InvokeAsync<IJSObjectReference>("attach", token, Frame, _reference,
                Options.ProxyUrl.AbsoluteUri, McpAppPolicy.Wire(new
                {
                    Tool = _invocation!.Binding.Tool.ProtocolTool,
                    Arguments = _invocation.Call.Arguments,
                    Options.MaximumHeight
                }), document, new
                {
                    Options.MaximumMessageBytes,
                    Options.MaximumPendingRequests,
                    InitializationTimeout = Options.InitializationTimeout.TotalMilliseconds,
                    OperationTimeout = Options.OperationTimeout.TotalMilliseconds,
                    TeardownTimeout = Options.TeardownTimeout.TotalMilliseconds
                });
        }
        catch (JSDisconnectedException error)
        {
            _lifetime.Cancel();
            _dispatcher.Dispose();
            Logger.LogDebug(error, "The MCP app renderer disconnected during initialization.");
            throw;
        }
        catch (JSException error)
        {
            _dispatcher.Dispose();
            Logger.LogWarning(error, "The MCP app bridge failed to initialize.");
            throw;
        }
        token.ThrowIfCancellationRequested();
        await OnReadyAsync(token);
    }

    private async ValueTask<McpAppResponse> HandleRequestAsync(McpAppRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await Host.DispatchAsync(_invocation!, request, Options, cancellationToken);
        }
        catch (McpProtocolException error)
        {
            return new(Error: new((int)error.ErrorCode, error.Message));
        }
        catch (InvalidOperationException error)
        {
            return new(Error: new(-32000, error.Message));
        }
    }

    /// <summary>Updates this app's MCP host-context fields while preserving fields omitted from the update.</summary>
    /// <param name="hostContext">A JSON object containing MCP Apps host-context fields to update.</param>
    /// <param name="cancellationToken">A token that cancels the interop operation.</param>
    protected async ValueTask UpdateHostContextAsync(JsonElement hostContext, CancellationToken cancellationToken = default)
    {
        if (hostContext.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("The MCP host-context update must be a JSON object.", nameof(hostContext));
        }
        if (Encoding.UTF8.GetByteCount(hostContext.GetRawText()) > Options.MaximumMessageBytes)
        {
            throw new InvalidOperationException("The MCP host-context update exceeds the configured byte limit.");
        }
        ObjectDisposedException.ThrowIf(_closed, this);
        var session = _session ?? throw new InvalidOperationException("The MCP app is not ready.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        deadline.CancelAfter(Options.OperationTimeout);
        try
        {
            await session.InvokeVoidAsync("updateHostContext", deadline.Token, hostContext);
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during a host-context update.");
            throw;
        }
        catch (JSException error)
        {
            Logger.LogWarning(error, "The MCP app host-context update failed.");
            throw;
        }
    }

    /// <summary>Closes this view and cancels outstanding work. A closed instance cannot be reopened.</summary>
    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource? completion = null;
        Task closing;
        lock (_closeLock)
        {
            if (_closing is null)
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _closing = completion.Task;
            }
            closing = _closing;
        }
        if (completion is not null)
        {
            _ = CompleteCloseAsync(completion);
        }
        return new(closing.WaitAsync(cancellationToken));
    }

    private async Task CompleteCloseAsync(TaskCompletionSource completion)
    {
        try
        {
            await CloseCoreAsync();
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }
    }

    private async Task CloseCoreAsync()
    {
        _closed = true;
        _lifetime.Cancel();
        _dispatcher?.Dispose();
        try
        {
            if (_initialization is not null)
            {
                try
                {
                    await _initialization;
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                }
            }
        }
        finally
        {
            try
            {
                await ReleaseBrowserAsync();
            }
            finally
            {
                _reference?.Dispose();
                await OnClosedAsync();
                if (_rendered && !_disposing)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
    }

    private async Task ReleaseBrowserAsync()
    {
        using var deadline = new CancellationTokenSource(Options.TeardownTimeout);
        try
        {
            try
            {
                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("detach", deadline.Token, Frame);
                }
            }
            finally
            {
                try
                {
                    if (_session is not null)
                    {
                        await _session.DisposeAsync().AsTask().WaitAsync(deadline.Token);
                    }
                }
                finally
                {
                    if (_module is not null)
                    {
                        await _module.DisposeAsync().AsTask().WaitAsync(deadline.Token);
                    }
                }
            }
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during cleanup.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposing = true;
        return CloseAsync();
    }

    private sealed class DispatchBridge(McpContentDispatcher dispatcher)
    {
        [JSInvokable]
        public Task<string> DispatchAsync(string id, string json) => dispatcher.DispatchAsync(id, json);

        [JSInvokable]
        public void Cancel(string id) => dispatcher.Cancel(id);
    }
}
