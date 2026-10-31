// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class UIActionHandler : ContentBlockHandler<UIActionHandler.State>
{
    private readonly IReadOnlyDictionary<string, AIFunction> _actions;

    internal UIActionHandler(IReadOnlyDictionary<string, AIFunction> actions)
    {
        _actions = actions;
    }

    public override BlockMappingResult<State> Handle(BlockMappingContext context, State state)
    {
        if (state.HasEmitted)
        {
            return BlockMappingResult<State>.Pass();
        }

        foreach (var content in context.UnhandledContents)
        {
            if (content is FunctionCallContent call &&
                !call.InformationalOnly &&
                _actions.TryGetValue(call.Name, out var function))
            {
                context.MarkHandled(call);
                state.HasEmitted = true;
                return BlockMappingResult<State>.Emit(
                    new UIActionBlock(function, call)
                    {
                        Id = call.CallId ?? Guid.NewGuid().ToString("N")
                    },
                    state);
            }
        }

        return BlockMappingResult<State>.Pass();
    }

    internal sealed class State
    {
        public bool HasEmitted { get; set; }
    }
}
