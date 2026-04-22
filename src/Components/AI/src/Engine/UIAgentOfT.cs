// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.AI;

public class UIAgent<TState> : UIAgent where TState : class, new()
{
    public UIAgent(IChatClient chatClient, TState? initialState = null)
        : base(chatClient)
    {
        State = new AgentState<TState>(initialState);
    }

    public UIAgent(IChatClient chatClient, ChatOptions chatOptions, TState? initialState = null)
        : base(chatClient, chatOptions)
    {
        State = new AgentState<TState>(initialState);
    }

    public UIAgent(IChatClient chatClient, Action<UIAgentOptions> configure, TState? initialState = null)
        : base(chatClient, configure)
    {
        State = new AgentState<TState>(initialState);
    }

    public UIAgent(IChatClient chatClient, Action<UIAgentOptions> configure, ILoggerFactory? loggerFactory, TState? initialState = null)
        : base(chatClient, configure, loggerFactory)
    {
        State = new AgentState<TState>(initialState);
    }

    public AgentState<TState> State { get; }

    internal override object? AgentStateObject => State;

    internal override ChatResponseUpdate ApplyStateMapper(ChatResponseUpdate update)
    {
        if (Options.StateMapper is null)
        {
            return update;
        }

        var context = new StateMapperContext(update);
        Options.StateMapper(context);

        if (context.StateValue is TState typedState)
        {
            State.Value = typedState;
        }

        return context.HasHandledContent ? context.GetFilteredUpdate() : update;
    }
}
