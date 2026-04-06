// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

internal sealed class DelegateBlockHandler<TState> : ContentBlockHandler<TState> where TState : new()
{
    private readonly Func<BlockMappingContext, TState, BlockMappingResult<TState>> _handler;

    internal DelegateBlockHandler(Func<BlockMappingContext, TState, BlockMappingResult<TState>> handler)
    {
        _handler = handler;
    }

    public override BlockMappingResult<TState> Handle(BlockMappingContext context, TState state)
    {
        return _handler(context, state);
    }
}
