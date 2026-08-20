// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class DataContentHandler : ContentBlockHandler<DataContentBlock>
{
    public override BlockMappingResult<DataContentBlock> Handle(
        BlockMappingContext context,
        DataContentBlock state)
    {
        if (state.Content is not null)
        {
            return BlockMappingResult<DataContentBlock>.Complete();
        }

        foreach (var content in context.UnhandledContents)
        {
            if (content is DataContent dataContent)
            {
                context.MarkHandled(dataContent);
                state.Content = dataContent;
                state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
                return BlockMappingResult<DataContentBlock>.Emit(state, state);
            }
        }

        return BlockMappingResult<DataContentBlock>.Pass();
    }
}
