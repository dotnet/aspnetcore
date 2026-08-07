// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class ReasoningHandler : ContentBlockHandler<ReasoningContentBlock>
{
    public override BlockMappingResult<ReasoningContentBlock> Handle(
        BlockMappingContext context, ReasoningContentBlock state)
    {
        TextReasoningContent? reasoningContent = null;
        foreach (var content in context.UnhandledContents)
        {
            if (content is TextReasoningContent trc)
            {
                reasoningContent = trc;
                break;
            }
        }

        if (reasoningContent is null)
        {
            if (state.Text.Length > 0 || state.ProtectedData is not null)
            {
                return BlockMappingResult<ReasoningContentBlock>.Complete();
            }

            return BlockMappingResult<ReasoningContentBlock>.Pass();
        }

        context.MarkHandled(reasoningContent);

        if (reasoningContent.ProtectedData is not null)
        {
            state.ProtectedData = reasoningContent.ProtectedData;
        }

        var text = reasoningContent.Text;
        if (!string.IsNullOrEmpty(text))
        {
            state.AppendText(text);
        }

        if (state.Text.Length == 0 && state.ProtectedData is null)
        {
            return BlockMappingResult<ReasoningContentBlock>.Pass();
        }

        if (state.LifecycleState != BlockLifecycleState.Active)
        {
            state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
            return BlockMappingResult<ReasoningContentBlock>.Emit(state, state);
        }
        else
        {
            return BlockMappingResult<ReasoningContentBlock>.Update(state);
        }
    }
}
