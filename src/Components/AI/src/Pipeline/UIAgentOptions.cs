// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Configures a <see cref="UIAgent"/>.
/// </summary>
/// <example>
/// <code>
/// var agent = new UIAgent(chatClient, options =>
/// {
///     options.ChatOptions = new ChatOptions { Instructions = "You are a helpful assistant." };
/// });
/// </code>
/// </example>
public class UIAgentOptions
{
    /// <summary>
    /// Gets or sets the options passed to the underlying <see cref="IChatClient"/>.
    /// </summary>
    public ChatOptions? ChatOptions { get; set; }

    /// <summary>
    /// Gets or sets a callback that maps model updates into typed agent state.
    /// </summary>
    public Action<StateMapperContext>? StateMapper { get; set; }

    /// <summary>
    /// Gets or sets the persistent conversation thread that receives completed turns.
    /// </summary>
    public IConversationThread? Thread { get; set; }

    internal List<IHandlerRegistration> HandlerRegistrations { get; } = new();

    internal Dictionary<string, AIFunction> UIActions { get; } =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a handler that maps model updates into content blocks. Registered handlers
    /// run before the built-in ones, so they can claim content the built-in handlers would
    /// otherwise map.
    /// </summary>
    /// <typeparam name="TState">The state the handler keeps across updates.</typeparam>
    /// <param name="handler">The handler to register.</param>
    public void AddBlockHandler<TState>(ContentBlockHandler<TState> handler)
        where TState : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        HandlerRegistrations.Add(new HandlerRegistration<TState>(handler));
    }

    /// <summary>
    /// Registers a function that a matching model tool call executes in the UI.
    /// The function is sent to the chat client as a declaration, not as an executable server tool.
    /// </summary>
    /// <param name="function">
    /// The function to execute in the UI. Its name must be unique among the registered UI actions.
    /// </param>
    /// <exception cref="ArgumentException">
    /// An action with the same name is already registered.
    /// </exception>
    public void RegisterUIAction(AIFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);
        UIActions.Add(function.Name, function);
    }

    internal interface IHandlerRegistration
    {
        IHandlerEntry CreateEntry();
    }

    private sealed class HandlerRegistration<TState> : IHandlerRegistration where TState : new()
    {
        private readonly ContentBlockHandler<TState> _handler;

        internal HandlerRegistration(ContentBlockHandler<TState> handler)
        {
            _handler = handler;
        }

        public IHandlerEntry CreateEntry() => new HandlerEntry<TState>(_handler);
    }
}

/// <summary>
/// Configures a <see cref="UIAgent{TState}"/>.
/// </summary>
/// <typeparam name="TState">The type of state associated with the agent.</typeparam>
public sealed class UIAgentOptions<TState> : UIAgentOptions where TState : class, new()
{
    internal UIAgentOptions(TState? initialState)
    {
        State = new AgentState<TState>(initialState);
    }

    /// <summary>
    /// Gets the observable state associated with the agent.
    /// </summary>
    public AgentState<TState> State { get; }
}
