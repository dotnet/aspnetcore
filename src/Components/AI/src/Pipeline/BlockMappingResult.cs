// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

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

    public static BlockMappingResult<TState> Pass() => new(ResultKind.Pass, null, default);

    public static BlockMappingResult<TState> Emit(ContentBlock block, TState state)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new(ResultKind.Emit, block, state);
    }

    public static BlockMappingResult<TState> Update(TState state) => new(ResultKind.Update, null, state);

    public static BlockMappingResult<TState> Complete() => new(ResultKind.Complete, null, default);
}
