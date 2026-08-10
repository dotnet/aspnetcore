// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class FunctionInvocationHandler : ContentBlockHandler<FunctionInvocationContentBlock>
{
    public override BlockMappingResult<FunctionInvocationContentBlock> Handle(
        BlockMappingContext context,
        FunctionInvocationContentBlock state)
    {
        if (state.Call is null)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionCallContent call)
                {
                    context.MarkHandled(call);
                    state.Call = call;
                    return BlockMappingResult<FunctionInvocationContentBlock>.Emit(state, state);
                }
            }
        }

        if (state.Call is not null)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionResultContent result &&
                    result.CallId == state.Call.CallId)
                {
                    context.MarkHandled(result);
                    state.Result = result;
                    return BlockMappingResult<FunctionInvocationContentBlock>.Complete();
                }
            }
        }

        return BlockMappingResult<FunctionInvocationContentBlock>.Pass();
    }
}
