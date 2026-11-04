// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed class McpAppBlockHandler(McpAppHost host) : ContentBlockHandler<McpAppContentBlock>
{
    public override BlockMappingResult<McpAppContentBlock> Handle(BlockMappingContext context, McpAppContentBlock state)
    {
        if (state.Result is not null)
        {
            return BlockMappingResult<McpAppContentBlock>.Complete();
        }

        var emit = false;
        if (state.Call is null)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionCallContent call && host.FindAppTool(call.Name) is not null)
                {
                    context.MarkHandled(call);
                    state.Call = call;
                    emit = true;
                    break;
                }
            }
        }
        if (state.Call is not null)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionResultContent result && result.CallId == state.Call.CallId)
                {
                    context.MarkHandled(result);
                    state.Result = result;
                    return emit ? BlockMappingResult<McpAppContentBlock>.Emit(state, state)
                        : BlockMappingResult<McpAppContentBlock>.Complete();
                }
            }
        }

        return emit ? BlockMappingResult<McpAppContentBlock>.Emit(state, state)
            : BlockMappingResult<McpAppContentBlock>.Pass();
    }
}
