// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// The outcome of a <see cref="ContentBlockHandler{TState}"/> invocation.
/// </summary>
/// <typeparam name="TState">The state the handler keeps across updates.</typeparam>
public readonly struct BlockMappingResult<TState>
{
    internal enum ResultKind { Pass, Emit, Update, Complete }

    internal ResultKind Kind { get; }

    internal ContentBlock? Block { get; }

    internal TState? State { get; }

    private BlockMappingResult(ResultKind kind, ContentBlock? block, TState? state)
    {
        Kind = kind;
        Block = block;
        State = state;
    }

    /// <summary>
    /// Indicates that the handler did not claim anything from the update.
    /// </summary>
    /// <returns>A result that leaves the update for other handlers.</returns>
    public static BlockMappingResult<TState> Pass() => new(ResultKind.Pass, null, default);

    /// <summary>
    /// Indicates that the handler produced a new block for the conversation.
    /// </summary>
    /// <param name="block">The block to add to the conversation.</param>
    /// <param name="state">The state to carry into subsequent updates.</param>
    /// <returns>A result that emits <paramref name="block"/>.</returns>
    public static BlockMappingResult<TState> Emit(ContentBlock block, TState state)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new(ResultKind.Emit, block, state);
    }

    /// <summary>
    /// Indicates that the handler updated the block it previously emitted.
    /// </summary>
    /// <param name="state">The updated state.</param>
    /// <returns>A result that notifies the UI that the block changed.</returns>
    public static BlockMappingResult<TState> Update(TState state) => new(ResultKind.Update, null, state);

    /// <summary>
    /// Indicates that the block the handler emitted is complete.
    /// </summary>
    /// <returns>A result that deactivates the block.</returns>
    public static BlockMappingResult<TState> Complete() => new(ResultKind.Complete, null, default);
}
