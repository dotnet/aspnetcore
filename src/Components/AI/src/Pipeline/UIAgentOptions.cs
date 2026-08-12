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

    internal List<IHandlerRegistration> HandlerRegistrations { get; } = new();

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
