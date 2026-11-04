// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Renders an original MCP invocation using an application-configured content host.</summary>
/// <typeparam name="THost">The application component that renders the iframe and configures MCP Apps hosting.</typeparam>
public sealed class McpBlock<THost> : ComponentBase, IDisposable where THost : McpContentHost
{
    private McpAppContentBlock? _renderedBlock;
    private McpAppHost? _renderedHost;
    private IDisposable? _subscription;

    /// <summary>Gets or sets the conversation-scoped app host.</summary>
    [Parameter, EditorRequired] public McpAppHost Host { get; set; } = default!;

    /// <summary>Gets or sets the originating function invocation block.</summary>
    [Parameter, EditorRequired] public McpAppContentBlock Block { get; set; } = default!;

    /// <summary>Gets or sets content displayed when an original raw result is unavailable after restoration.</summary>
    [Parameter] public RenderFragment? UnavailableContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Host);
        ArgumentNullException.ThrowIfNull(Block);
        if (_renderedBlock is not null &&
            (_renderedBlock.Id != Block.Id ||
                _renderedBlock.ToolName != Block.ToolName || !ReferenceEquals(_renderedHost, Host)))
        {
            throw new InvalidOperationException("Use @key with the originating call ID when replacing an MCP app.");
        }
        if (!ReferenceEquals(_renderedBlock, Block))
        {
            _subscription?.Dispose();
            _renderedBlock = Block;
            _renderedHost = Host;
            _subscription = Block.OnChanged(() => _ = InvokeAsync(StateHasChanged));
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Host.FindInvocation(Block) is not null)
        {
            builder.OpenComponent<THost>(0);
            builder.AddAttribute(1, nameof(McpContentHost.Host), Host);
            builder.AddAttribute(2, nameof(McpContentHost.Block), Block);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "role", "status");
            if (Block.HasResult && UnavailableContent is not null)
            {
                builder.AddContent(6, UnavailableContent);
            }
            else
            {
                builder.AddContent(7, Block.HasResult
                    ? "The original app result is unavailable. The tool will not be run again."
                    : "Waiting for the original tool invocation.");
            }
            builder.CloseElement();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
