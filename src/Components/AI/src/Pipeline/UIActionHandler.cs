// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class UIActionHandler : ContentBlockHandler<UIActionHandler.UIActionHandlerState>
{
    private readonly IReadOnlyDictionary<string, AIFunction> _uiActions;

    internal UIActionHandler(IReadOnlyDictionary<string, AIFunction> uiActions)
    {
        _uiActions = uiActions;
    }

    public override BlockMappingResult<UIActionHandlerState> Handle(
        BlockMappingContext context, UIActionHandlerState state)
    {
        foreach (var content in context.UnhandledContents)
        {
            if (content is FunctionCallContent fcc && _uiActions.TryGetValue(fcc.Name, out var function))
            {
                context.MarkHandled(fcc);
                var innerBlock = new FunctionInvocationContentBlock();
                innerBlock.Call = fcc;
                innerBlock.Id = fcc.CallId ?? Guid.NewGuid().ToString("N");
                var block = new UIActionBlock(function, innerBlock);
                block.Id = innerBlock.Id;
                return BlockMappingResult<UIActionHandlerState>.Emit(block, state);
            }
        }

        return BlockMappingResult<UIActionHandlerState>.Pass();
    }

    internal sealed class UIActionHandlerState
    {
    }
}
