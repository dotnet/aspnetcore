// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Provides MCP Apps communication and lifetime management for an application-rendered iframe.</summary>
/// <remarks>Derive a Razor component from this type and render an iframe with <c>@ref="Frame"</c> and a distinct-origin sandbox URL.</remarks>
public abstract class McpContentHost : ComponentBase, IAsyncDisposable
{
    internal const string ModulePath = "./_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-host.js";
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _closeLock = new();
    private McpContentDispatcher? _dispatcher;
    private DotNetObjectReference<DispatchBridge>? _reference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _session;
    private McpClientTool? _tool;
    private McpClient? _renderedClient;
    private string? _callId;
    private string _resourceUri = default!;
    private Task? _initialization;
    private Task? _resultDelivery;
    private AgentContext? _subscribedContext;
    private ConversationTurn? _turn;
    private IDisposable? _statusSubscription;
    private Task? _closing;
    private volatile bool _closed;
    private bool _disposing;
    private bool _rendered;

    /// <summary>Gets the browser interop runtime for additional application-owned functionality.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<McpContentHost> Logger { get; set; } = default!;

    /// <summary>Gets or sets the retained originating invocation block.</summary>
    [Parameter, EditorRequired] public FunctionInvocationContentBlock Block { get; set; } = default!;

    /// <summary>Gets or sets the application-owned client cascaded by this app's <see cref="McpBlock{THost}"/>.</summary>
    [CascadingParameter] public McpClient Client { get; set; } = default!;

    /// <summary>Gets or sets the ambient conversation state used to observe turn failure and cancellation.</summary>
    [CascadingParameter] public AgentContext? AgentContext { get; set; }

    /// <summary>Gets or sets the already-discovered SDK tool snapshot, including model-facing aliases.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<McpClientTool> Tools { get; set; } = default!;

    /// <summary>Gets or sets the application-rendered iframe reference.</summary>
    protected ElementReference Frame { get; set; }

    /// <summary>Gets whether this host has closed and should stop rendering its iframe.</summary>
    protected bool IsClosed => _closed;

    /// <summary>Runs after the MCP app has initialized.</summary>
    protected virtual ValueTask OnReadyAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>Runs after the view has closed.</summary>
    protected virtual ValueTask OnClosedAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_closed)
        {
            return;
        }
        ArgumentNullException.ThrowIfNull(Block);
        ArgumentNullException.ThrowIfNull(Client);
        ArgumentNullException.ThrowIfNull(Tools);
        var tool = Tools.SingleOrDefault(tool => tool.Name == Block.ToolName)
            ?? throw new InvalidOperationException("The originating tool is missing from the supplied SDK snapshot.");
        var resourceUri = GetResourceUri(tool.ProtocolTool)
            ?? throw new InvalidOperationException("The originating tool has no MCP app resource.");
        if (_tool is not null && (!ReferenceEquals(_tool, tool) || _resourceUri != resourceUri ||
            _callId != Block.Id || !ReferenceEquals(_renderedClient, Client)))
        {
            throw new InvalidOperationException("Use @key with the originating call ID when replacing an MCP app.");
        }
        if (!ReferenceEquals(_subscribedContext, AgentContext))
        {
            _statusSubscription?.Dispose();
            _subscribedContext = AgentContext;
            _turn = null;
            _statusSubscription = AgentContext?.RegisterOnStatusChanged(status => _ = InvokeAsync(StateHasChanged));
        }
        _turn ??= AgentContext?.Turns.FirstOrDefault(turn =>
            turn.ResponseBlocks.Any(block => ReferenceEquals(block, Block)));
        _tool = tool;
        _renderedClient = Client;
        _callId = Block.Id;
        _resourceUri = resourceUri;
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
        return DeliverResultAsync();
    }

    private async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Frame.Id))
        {
            throw new InvalidOperationException("The MCP content host must render an iframe with @ref=\"Frame\".");
        }
        var token = _lifetime.Token;
        var document = await LoadDocumentAsync(token).AsTask().WaitAsync(token);
        _dispatcher = new(DispatchAsync);
        _reference = DotNetObjectReference.Create(new DispatchBridge(_dispatcher));
        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", token,
                ModulePath);
            token.ThrowIfCancellationRequested();
            _session = await _module.InvokeAsync<IJSObjectReference>("attach", token, Frame, _reference,
                JsonSerializer.SerializeToElement(new
                {
                    Tool = _tool!.ProtocolTool,
                    Arguments = Block.Arguments
                }, McpJsonUtilities.DefaultOptions), document);
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
        await DeliverResultAsync();
        await OnReadyAsync(token);
    }

    private Task DeliverResultAsync()
    {
        if (_closed || _session is null)
        {
            return Task.CompletedTask;
        }
        if (Block.Result is { } result)
        {
            return _resultDelivery ??= SendResultAsync(result);
        }
        if (_turn is not null && !_turn.ResponseBlocks.Any(block => ReferenceEquals(block, Block)))
        {
            return _resultDelivery ??= SendCancelledAsync("The originating model turn was cancelled.");
        }
        if (AgentContext is { Status: ConversationStatus.Error } context &&
            ReferenceEquals(context.Turns.LastOrDefault(), _turn))
        {
            return _resultDelivery ??= SendCancelledAsync(
                context.Error?.Message ?? "The originating model turn failed.");
        }
        if (_turn is not null && AgentContext is { Status: ConversationStatus.Idle } idle &&
            ReferenceEquals(idle.Turns.LastOrDefault(), _turn))
        {
            return _resultDelivery ??= SendCancelledAsync("The originating tool call ended without a result.");
        }
        return Task.CompletedTask;
    }

    private async Task SendResultAsync(Microsoft.Extensions.AI.FunctionResultContent result)
    {
        if (result.Exception is { } exception)
        {
            await SendCancelledAsync(exception is OperationCanceledException
                ? "The originating tool call was cancelled." : exception.Message);
            return;
        }
        JsonElement payload;
        try
        {
            payload = PrepareResult(result.Result);
        }
        catch (InvalidOperationException error)
        {
            Logger.LogWarning(error, "The original tool result for {CallId} cannot be delivered to the MCP app.", Block.Id);
            await SendCancelledAsync(error.Message);
            return;
        }
        try
        {
            await _session!.InvokeVoidAsync("sendToolResult", _lifetime.Token, payload);
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during result delivery.");
            throw;
        }
        catch (JSException error)
        {
            Logger.LogWarning(error, "The MCP app result delivery failed.");
            throw;
        }
    }

    private async Task SendCancelledAsync(string reason)
    {
        try
        {
            await _session!.InvokeVoidAsync("sendToolCancelled", _lifetime.Token, reason);
        }
        catch (JSDisconnectedException error)
        {
            Logger.LogDebug(error, "The MCP app renderer disconnected during cancellation delivery.");
            throw;
        }
        catch (JSException error)
        {
            Logger.LogWarning(error, "The MCP app cancellation delivery failed.");
            throw;
        }
    }

    private async ValueTask<AppDocument> LoadDocumentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Resource? listing = null;
        if (Client.ServerCapabilities.Resources is not null)
        {
            var resources = await Client.ListResourcesAsync(cancellationToken: cancellationToken);
            listing = resources.FirstOrDefault(resource => resource.Uri == _resourceUri)?.ProtocolResource;
        }
        var result = await Client.ReadResourceAsync(_resourceUri, cancellationToken: cancellationToken);
        var documents = result.Contents.Where(content => content.Uri == _resourceUri &&
            content.MimeType == McpAppClientOptionsExtensions.HtmlMimeType).ToArray();
        if (documents.Length != 1)
        {
            throw new InvalidOperationException("The app resource must contain exactly one matching HTML document.");
        }
        var selected = documents[0];
        var html = selected switch
        {
            TextResourceContents text => text.Text,
            BlobResourceContents blob => new UTF8Encoding(false, true).GetString(blob.DecodedData.Span),
            _ => throw new InvalidOperationException("The app document encoding is not supported.")
        };
        var listingUi = GetUi(listing?.Meta);
        var contentUi = GetUi(selected.Meta);
        return new(html, GetDeclaration(contentUi, listingUi, "csp"),
            GetDeclaration(contentUi, listingUi, "permissions"));
    }

    internal async ValueTask<JsonRpcMessage> DispatchAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await DispatchCoreAsync(request, cancellationToken);
        }
        catch (JsonException error)
        {
            return Error(request.Id, -32602, error.Message);
        }
        catch (McpProtocolException error)
        {
            return Error(request.Id, (int)error.ErrorCode, error.Message);
        }
        catch (InvalidOperationException error)
        {
            return Error(request.Id, -32000, error.Message);
        }
    }

    private static JsonRpcError Error(RequestId id, int code, string message) =>
        new() { Id = id, Error = new() { Code = code, Message = message } };

    private async ValueTask<JsonRpcMessage> DispatchCoreAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        lifetime.Token.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(request.Method))
        {
            throw new JsonException("An app request method is required.");
        }
        if (request.Params is not JsonObject parameters)
        {
            throw new JsonException("App request parameters must be a JSON object.");
        }
        object result;
        switch (request.Method)
        {
            case RequestMethods.ToolsList:
                RejectCursor(parameters.Deserialize<ListToolsRequestParams>(McpJsonUtilities.DefaultOptions)!.Cursor);
                result = new ListToolsResult
                {
                    Tools = Tools.Where(CanCall).Select(tool => tool.ProtocolTool).ToList()
                };
                break;
            case RequestMethods.ToolsCall:
                var call = parameters.Deserialize<CallToolRequestParams>(McpJsonUtilities.DefaultOptions)!;
                var name = call.Name;
                if (string.IsNullOrEmpty(name))
                {
                    throw new JsonException("A nonempty tool name is required.");
                }
                var tool = Tools.FirstOrDefault(tool => tool.ProtocolTool.Name == name);
                if (tool is null || !CanCall(tool))
                {
                    throw new InvalidOperationException("The requested app tool is not available on the owning connection.");
                }
                if (parameters.ContainsKey("arguments") && parameters["arguments"] is not JsonObject)
                {
                    throw new JsonException("App tool arguments must be a JSON object.");
                }
                var dictionary = call.Arguments?.ToDictionary(property => property.Key,
                    property => (object?)property.Value, StringComparer.Ordinal);
                result = await tool.CallAsync(dictionary, cancellationToken: lifetime.Token);
                break;
            case RequestMethods.ResourcesRead:
                var uri = parameters.Deserialize<ReadResourceRequestParams>(McpJsonUtilities.DefaultOptions)!.Uri;
                if (string.IsNullOrEmpty(uri))
                {
                    throw new JsonException("A nonempty resource URI is required.");
                }
                if (uri != _resourceUri)
                {
                    throw new InvalidOperationException("The requested resource is not permitted by this host.");
                }
                result = await Client.ReadResourceAsync(uri, cancellationToken: lifetime.Token);
                break;
            case RequestMethods.ResourcesList:
                RejectCursor(parameters.Deserialize<ListResourcesRequestParams>(McpJsonUtilities.DefaultOptions)!.Cursor);
                var resources = await Client.ListResourcesAsync(cancellationToken: lifetime.Token);
                result = new ListResourcesResult
                {
                    Resources = resources.Where(resource => resource.Uri == _resourceUri)
                        .Select(resource => resource.ProtocolResource).ToList()
                };
                break;
            default:
                return Error(request.Id, -32601, $"Unsupported app operation: {request.Method}.");
        }
        return new JsonRpcResponse { Id = request.Id, Result = JsonSerializer.SerializeToNode(result, McpJsonUtilities.DefaultOptions) };
    }

    private static bool CanCall(McpClientTool tool)
    {
        var ui = GetUi(tool.ProtocolTool.Meta);
        if (ui is null || !ui.TryGetPropertyValue("visibility", out var visibility))
        {
            return true;
        }
        if (visibility is not JsonArray audiences || audiences.Any(item =>
            item is not JsonValue value || !value.TryGetValue<string>(out var audience) ||
            audience is not ("model" or "app")))
        {
            throw new InvalidOperationException("Tool UI visibility must contain only model or app.");
        }
        return audiences.Any(item => item!.GetValue<string>() == "app");
    }

    private static JsonObject? GetUi(JsonObject? metadata)
    {
        if (metadata?["ui"] is not { } ui)
        {
            return null;
        }
        return ui as JsonObject ?? throw new InvalidOperationException("App UI metadata must be an object.");
    }

    private static string? GetResourceUri(Tool tool)
    {
        JsonNode? resource;
        if (GetUi(tool.Meta) is not { } ui || !ui.TryGetPropertyValue("resourceUri", out resource))
        {
            if (tool.Meta is null || !tool.Meta.TryGetPropertyValue("ui/resourceUri", out resource))
            {
                return null;
            }
        }
        if (resource is null)
        {
            return null;
        }
        if (resource is not JsonValue value || !value.TryGetValue<string>(out var text) ||
            !text.StartsWith("ui://", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != "ui")
        {
            throw new InvalidOperationException("App tools must reference an absolute ui:// resource.");
        }
        return text;
    }

    private static JsonElement? GetDeclaration(JsonObject? content, JsonObject? listing, string name)
    {
        JsonNode? declaration;
        if (content is null || !content.TryGetPropertyValue(name, out declaration))
        {
            if (listing is null || !listing.TryGetPropertyValue(name, out declaration))
            {
                return null;
            }
        }
        if (declaration is not JsonObject)
        {
            throw new InvalidOperationException($"The app {name} declaration must be an object.");
        }
        return JsonSerializer.SerializeToElement(declaration, McpJsonUtilities.DefaultOptions);
    }

    private static JsonElement PrepareResult(object? result) =>
        result switch
        {
            JsonElement envelope when envelope.ValueKind == JsonValueKind.Object &&
                envelope.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array =>
                envelope.Clone(),
            CallToolResult protocolResult => JsonSerializer.SerializeToElement(protocolResult, McpJsonUtilities.DefaultOptions),
            AIContent item => JsonSerializer.SerializeToElement(new CallToolResult { Content = [ToContentBlock(item)] },
                McpJsonUtilities.DefaultOptions),
            IEnumerable<AIContent> items => JsonSerializer.SerializeToElement(new CallToolResult
            {
                Content = items.Select(ToContentBlock).ToArray()
            }, McpJsonUtilities.DefaultOptions),
            _ => throw new InvalidOperationException(
                $"The SDK tool result shape '{result?.GetType().FullName ?? "null"}' cannot be delivered as an MCP app result.")
        };

    private static ModelContextProtocol.Protocol.ContentBlock ToContentBlock(AIContent content) =>
        content.RawRepresentation is ModelContextProtocol.Protocol.ContentBlock original ? original :
        content is TextContent or DataContent ? content.ToContentBlock() :
        throw new InvalidOperationException($"The AI content type '{content.GetType().FullName}' has no MCP content representation.");

    private static void RejectCursor(string? cursor)
    {
        if (cursor is not null)
        {
            throw new JsonException("The supplied tool snapshot has no continuation cursor.");
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
        ObjectDisposedException.ThrowIf(_closed, this);
        var session = _session ?? throw new InvalidOperationException("The MCP app is not ready.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        try
        {
            await session.InvokeVoidAsync("updateHostContext", lifetime.Token, hostContext);
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
        _statusSubscription?.Dispose();
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
            if (_resultDelivery is not null)
            {
                try
                {
                    await _resultDelivery;
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
        try
        {
            try
            {
                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("detach", Frame);
                }
            }
            finally
            {
                try
                {
                    if (_session is not null)
                    {
                        await _session.DisposeAsync();
                    }
                }
                finally
                {
                    if (_module is not null)
                    {
                        await _module.DisposeAsync();
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

    private readonly struct AppDocument(string html, JsonElement? csp, JsonElement? permissions)
    {
        public string Html { get; } = html;
        public JsonElement? Csp { get; } = csp;
        public JsonElement? Permissions { get; } = permissions;
    }

    private sealed class DispatchBridge(McpContentDispatcher dispatcher)
    {
        [JSInvokable]
        public Task<string> DispatchAsync(string json) => dispatcher.DispatchAsync(json);

        [JSInvokable]
        public void Cancel(string id) => dispatcher.Cancel(new RequestId(id));
    }
}
