// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class DataContentHandler : ContentBlockHandler<MediaContentBlock>
{
    public override BlockMappingResult<MediaContentBlock> Handle(
        BlockMappingContext context,
        MediaContentBlock state)
    {
        if (state.Content is not null)
        {
            return BlockMappingResult<MediaContentBlock>.Complete();
        }

        foreach (var content in context.UnhandledContents)
        {
            if (content is DataContent dataContent)
            {
                context.MarkHandled(dataContent);
                state.Content = MediaContentFactory.Create(dataContent);
                state.Name = dataContent.Name;
                state.Id = Guid.NewGuid().ToString("N");
                return BlockMappingResult<MediaContentBlock>.Emit(state, state);
            }
        }

        return BlockMappingResult<MediaContentBlock>.Pass();
    }
}
