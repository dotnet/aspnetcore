// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class UIAgentOptions
{
    public ChatOptions? ChatOptions { get; set; }

    public Func<StateMapperContext, bool>? StateMapper { get; set; }

    public IConversationThread? Thread { get; set; }

    internal List<IHandlerRegistration> HandlerRegistrations { get; } = new();

    internal Dictionary<string, AIFunction> UIActions { get; } = new();

    public void AddBlockHandler<TState>(ContentBlockHandler<TState> handler)
        where TState : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        HandlerRegistrations.Add(new HandlerRegistration<TState>(handler));
    }

    public void RegisterUIAction(AIFunction function)
    {
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
