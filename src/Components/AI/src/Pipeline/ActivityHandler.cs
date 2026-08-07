// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public abstract class ActivityHandler<TBlock> : ContentBlockHandler<TBlock>
    where TBlock : ActivityContentBlock, new()
{
    public sealed override BlockMappingResult<TBlock> Handle(
        BlockMappingContext context, TBlock state)
    {
        if (state.Id == string.Empty)
        {
            if (TryCreateBlock(context, state))
            {
                OnContentUpdated(state);
                return BlockMappingResult<TBlock>.Emit(state, state);
            }

            return BlockMappingResult<TBlock>.Pass();
        }

        if (TryUpdateBlock(context, state, out var isCompleted))
        {
            OnContentUpdated(state);

            if (isCompleted)
            {
                return BlockMappingResult<TBlock>.Complete();
            }

            return BlockMappingResult<TBlock>.Update(state);
        }

        return BlockMappingResult<TBlock>.Pass();
    }

    protected abstract bool TryCreateBlock(BlockMappingContext context, TBlock state);

    protected abstract bool TryUpdateBlock(
        BlockMappingContext context, TBlock state, out bool isCompleted);

    protected virtual void OnContentUpdated(TBlock state) { }
}
