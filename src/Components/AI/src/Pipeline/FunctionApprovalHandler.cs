// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class FunctionApprovalHandler :
    ContentBlockHandler<FunctionApprovalHandler.State>
{
    public override BlockMappingResult<State> Handle(BlockMappingContext context, State state)
    {
        if (state.Emitted)
        {
            return BlockMappingResult<State>.Complete();
        }

        foreach (var content in context.UnhandledContents)
        {
            if (content is not ToolApprovalRequestContent approvalRequest)
            {
                continue;
            }

            context.MarkHandled(approvalRequest);

            var innerBlock = context.CreateInnerBlock(approvalRequest.ToolCall)
                ?? CreateFallbackInnerBlock(approvalRequest.ToolCall);

            state.Emitted = true;
            return BlockMappingResult<State>.Emit(
                new FunctionApprovalBlock(innerBlock, approvalRequest)
                {
                    Id = approvalRequest.ToolCall is FunctionCallContent functionCall
                        ? functionCall.CallId ?? approvalRequest.RequestId
                        : approvalRequest.RequestId
                },
                state);
        }

        return BlockMappingResult<State>.Pass();
    }

    private static FunctionInvocationContentBlock CreateFallbackInnerBlock(
        ToolCallContent toolCall)
    {
        var block = new FunctionInvocationContentBlock();
        if (toolCall is FunctionCallContent functionCall)
        {
            block.Call = functionCall;
        }

        return block;
    }

    internal sealed class State
    {
        internal bool Emitted { get; set; }
    }
}
