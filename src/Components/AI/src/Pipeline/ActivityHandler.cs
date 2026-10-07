// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Maps activity snapshots and updates into a mutable content block.
/// </summary>
/// <typeparam name="TBlock">The activity block type.</typeparam>
public abstract class ActivityHandler<TBlock> : ContentBlockHandler<TBlock>
    where TBlock : ActivityContentBlock, new()
{
    /// <inheritdoc />
    public sealed override BlockMappingResult<TBlock> Handle(
        BlockMappingContext context,
        TBlock state)
    {
        if (state.Id == string.Empty)
        {
            if (TryCreateBlock(context, state))
            {
                if (state.Id.Length == 0)
                {
                    state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
                }

                OnContentUpdated(state);
                return BlockMappingResult<TBlock>.Emit(state, state);
            }

            return BlockMappingResult<TBlock>.Pass();
        }

        if (TryUpdateBlock(context, state, out var isCompleted))
        {
            OnContentUpdated(state);
            return isCompleted
                ? BlockMappingResult<TBlock>.Complete()
                : BlockMappingResult<TBlock>.Update(state);
        }

        return BlockMappingResult<TBlock>.Pass();
    }

    /// <summary>
    /// Attempts to initialize a block from an activity snapshot.
    /// </summary>
    /// <param name="context">The update being mapped.</param>
    /// <param name="state">The block to initialize.</param>
    /// <returns><see langword="true"/> when the update created the block.</returns>
    protected abstract bool TryCreateBlock(BlockMappingContext context, TBlock state);

    /// <summary>
    /// Attempts to update an existing activity block.
    /// </summary>
    /// <param name="context">The update being mapped.</param>
    /// <param name="state">The block to update.</param>
    /// <param name="isCompleted">Whether the update completes the activity.</param>
    /// <returns><see langword="true"/> when the update belongs to the block.</returns>
    protected abstract bool TryUpdateBlock(
        BlockMappingContext context,
        TBlock state,
        out bool isCompleted);

    /// <summary>
    /// Updates derived block state after the activity payload changes.
    /// </summary>
    /// <param name="state">The updated activity block.</param>
    protected virtual void OnContentUpdated(TBlock state)
    {
    }
}
