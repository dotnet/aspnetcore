// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class MediaContentHandler : ContentBlockHandler<MediaContentBlock>
{
    public override BlockMappingResult<MediaContentBlock> Handle(
        BlockMappingContext context, MediaContentBlock state)
    {
        DataContent? dataContent = null;
        foreach (var content in context.UnhandledContents)
        {
            if (content is DataContent dc)
            {
                dataContent = dc;
                break;
            }
        }

        if (dataContent is null)
        {
            if (state.Items.Count > 0)
            {
                return BlockMappingResult<MediaContentBlock>.Complete();
            }
            return BlockMappingResult<MediaContentBlock>.Pass();
        }

        context.MarkHandled(dataContent);

        if (state.Items.Count == 0)
        {
            state.AddContent(dataContent);
            state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
            return BlockMappingResult<MediaContentBlock>.Emit(state, state);
        }
        else
        {
            state.AddContent(dataContent);
            return BlockMappingResult<MediaContentBlock>.Update(state);
        }
    }
}
