// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Turns an <see cref="IChatClient"/> stream into renderable content blocks and typed observable state.
/// </summary>
/// <typeparam name="TState">The type of state associated with the agent.</typeparam>
public class UIAgent<TState> : UIAgent where TState : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent{TState}"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="initialState">The initial state value.</param>
    public UIAgent(IChatClient chatClient, TState? initialState = null)
        : this(chatClient, new UIAgentOptions<TState>(initialState), configure: null, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent{TState}"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="chatOptions">The options passed to the chat client.</param>
    /// <param name="initialState">The initial state value.</param>
    public UIAgent(IChatClient chatClient, ChatOptions chatOptions, TState? initialState = null)
        : this(
            chatClient,
            new UIAgentOptions<TState>(initialState),
            options => options.ChatOptions = chatOptions,
            loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent{TState}"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="configure">A callback that configures the agent.</param>
    /// <param name="initialState">The initial state value.</param>
    public UIAgent(
        IChatClient chatClient,
        Action<UIAgentOptions<TState>> configure,
        TState? initialState = null)
        : this(chatClient, new UIAgentOptions<TState>(initialState), configure, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent{TState}"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="configure">A callback that configures the agent.</param>
    /// <param name="loggerFactory">The logger factory used to trace block mapping.</param>
    /// <param name="initialState">The initial state value.</param>
    public UIAgent(
        IChatClient chatClient,
        Action<UIAgentOptions<TState>> configure,
        ILoggerFactory? loggerFactory,
        TState? initialState = null)
        : this(chatClient, new UIAgentOptions<TState>(initialState), configure, loggerFactory)
    {
    }

    private UIAgent(
        IChatClient chatClient,
        UIAgentOptions<TState> options,
        Action<UIAgentOptions<TState>>? configure,
        ILoggerFactory? loggerFactory)
        : base(chatClient, options, loggerFactory)
    {
        State = options.State;
        configure?.Invoke(options);
    }

    /// <summary>
    /// Gets the observable state associated with this agent.
    /// </summary>
    public AgentState<TState> State { get; }

    internal override ChatResponseUpdate ApplyStateMapper(ChatResponseUpdate update)
    {
        if (Options.StateMapper is null)
        {
            return update;
        }

        var context = new StateMapperContext(update);
        Options.StateMapper(context);

        if (context.StateValue is not null)
        {
            if (context.StateValue is not TState typedState)
            {
                throw new InvalidOperationException(
                    $"The state mapper returned a value of type '{context.StateValue.GetType()}', " +
                    $"but this agent requires state of type '{typeof(TState)}'.");
            }

            State.Value = typedState;
        }

        return context.HasHandledContent ? context.GetFilteredUpdate() : update;
    }
}
