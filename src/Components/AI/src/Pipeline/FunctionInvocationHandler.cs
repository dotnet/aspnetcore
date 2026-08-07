// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class FunctionInvocationHandler : ContentBlockHandler<FunctionInvocationContentBlock>
{
    public override BlockMappingResult<FunctionInvocationContentBlock> Handle(
        BlockMappingContext context, FunctionInvocationContentBlock state)
    {
        // Check for FunctionCallContent — only when not already tracking a call
        if (state.Call is null)
        {
            FunctionCallContent? callContent = null;
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionCallContent fc)
                {
                    callContent = fc;
                    break;
                }
            }

            if (callContent is not null)
            {
                context.MarkHandled(callContent);
                state.Call = callContent;
                state.Id = callContent.CallId;
                return BlockMappingResult<FunctionInvocationContentBlock>.Emit(state, state);
            }
        }

        // Check for FunctionResultContent matching our active block's CallId
        FunctionResultContent? resultContent = null;
        foreach (var content in context.UnhandledContents)
        {
            if (content is FunctionResultContent frc)
            {
                resultContent = frc;
                break;
            }
        }

        if (resultContent is not null && state.Call is not null && resultContent.CallId == state.Call.CallId)
        {
            context.MarkHandled(resultContent);
            state.Result = resultContent;
            return BlockMappingResult<FunctionInvocationContentBlock>.Complete();
        }

        // No matching content — wait
        return BlockMappingResult<FunctionInvocationContentBlock>.Pass();
    }
}
