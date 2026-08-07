// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class HandlerEntry<TState> : IHandlerEntry where TState : new()
{
    private readonly ContentBlockHandler<TState> _handler;

    internal HandlerEntry(ContentBlockHandler<TState> handler)
    {
        _handler = handler;
    }

    public IActiveEntry? TryHandle(BlockMappingContext context)
    {
        var state = new TState();
        var result = _handler.Handle(context, state);
        if (result.Kind == BlockMappingResult<TState>.ResultKind.Emit)
        {
            return new ActiveEntry(_handler, result.State!, result.Block!);
        }
        return null;
    }

    private sealed class ActiveEntry : IActiveEntry
    {
        private readonly ContentBlockHandler<TState> _handler;
        private TState _state;

        internal ActiveEntry(
            ContentBlockHandler<TState> handler,
            TState state,
            ContentBlock block)
        {
            _handler = handler;
            _state = state;
            Block = block;
        }

        public ContentBlock Block { get; }

        public HandleResult Invoke(BlockMappingContext context)
        {
            var result = _handler.Handle(context, _state);
            switch (result.Kind)
            {
                case BlockMappingResult<TState>.ResultKind.Update:
                    _state = result.State!;
                    return HandleResult.Update();

                case BlockMappingResult<TState>.ResultKind.Complete:
                    return HandleResult.Complete();

                default:
                    return HandleResult.Pass();
            }
        }
    }
}
