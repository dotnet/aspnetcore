// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

public class AgentBoundary : ComponentBase, IDisposable
{
    private AgentContext _context = default!;
    private UIAgent _currentAgent = default!;
    private object? _agentState;
    private Type? _cascadingValueType;

    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        _currentAgent = Agent;
        _context = new AgentContext(Agent);
        _agentState = Agent.AgentStateObject;

        if (_agentState is not null)
        {
            _cascadingValueType = typeof(CascadingValue<>).MakeGenericType(_agentState.GetType());
        }
    }

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Agent, _currentAgent))
        {
            // Agent changed — tear down the old context and create a new one.
            // BuildRenderTree uses OpenRegion keyed on the agent, so Blazor will
            // also tear down and recreate all descendant components.
            _context?.Dispose();
            _currentAgent = Agent;
            _context = new AgentContext(Agent);
            _agentState = Agent.AgentStateObject;

            if (_agentState is not null)
            {
                _cascadingValueType = typeof(CascadingValue<>).MakeGenericType(_agentState.GetType());
            }
            else
            {
                _cascadingValueType = null;
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var thread = Agent.Options.Thread;
        if (thread is not null && thread.GetUpdates().Count > 0)
        {
            await _context.RestoreAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // If Agent changes, the region key changes, causing Blazor to tear down
        // and recreate all descendants. This is the same trick EditForm uses with
        // EditContext. It lets us safely use IsFixed=true on the CascadingValue.
        builder.OpenRegion(_context.GetHashCode());

        // Outer: cascade AgentContext
        builder.OpenComponent<CascadingValue<AgentContext>>(0);
        builder.AddComponentParameter(1, "Value", _context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(inner =>
        {
            if (_agentState is not null)
            {
                // Inner: cascade AgentState<TState> with the correct generic type
                inner.OpenComponent(10, _cascadingValueType!);
                inner.AddComponentParameter(11, "Value", _agentState);
                inner.AddComponentParameter(12, "IsFixed", true);
                inner.AddComponentParameter(13, "ChildContent", (RenderFragment)(stateInner =>
                {
                    stateInner.AddContent(20, ChildContent);
                }));
                inner.CloseComponent();
            }
            else
            {
                inner.AddContent(10, ChildContent);
            }
        }));
        builder.CloseComponent();

        builder.CloseRegion();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
