// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Creates the <see cref="AgentContext"/> for a <see cref="UIAgent"/> and cascades it to the
/// chat components underneath it.
/// </summary>
public class AgentBoundary : ComponentBase, IDisposable
{
    private AgentContext _context = default!;
    private UIAgent _currentAgent = default!;

    /// <summary>
    /// Gets or sets the agent that drives the conversation.
    /// </summary>
    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    /// <summary>
    /// Gets or sets the content rendered inside the boundary.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _currentAgent = Agent;
        _context = new AgentContext(Agent);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Agent, _currentAgent))
        {
            // Agent changed — tear down the old context and create a new one.
            // BuildRenderTree uses OpenRegion keyed on the context, so Blazor will
            // also tear down and recreate all descendant components.
            _context?.Dispose();
            _currentAgent = Agent;
            _context = new AgentContext(Agent);
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // If Agent changes, the region key changes, causing Blazor to tear down
        // and recreate all descendants. This is the same trick EditForm uses with
        // EditContext. It lets us safely use IsFixed=true on the CascadingValue.
        builder.OpenRegion(_context.GetHashCode());

        builder.OpenComponent<CascadingValue<AgentContext>>(0);
        builder.AddComponentParameter(1, "Value", _context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(inner =>
        {
            inner.AddContent(10, ChildContent);
        }));
        builder.CloseComponent();

        builder.CloseRegion();
    }

    /// <summary>
    /// Disposes the <see cref="AgentContext"/> owned by this boundary.
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
