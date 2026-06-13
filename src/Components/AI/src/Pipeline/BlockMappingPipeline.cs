// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

internal class BlockMappingPipeline
{
    private readonly List<IHandlerEntry> _handlers = new();
    private readonly List<IActiveEntry> _activeStack = new();

    internal BlockMappingPipeline(UIAgentOptions options)
    {
        // User-registered handlers go first so they can customize behavior
        foreach (var registration in options.HandlerRegistrations)
        {
            _handlers.Add(registration.CreateEntry());
        }

        // Built-in text handler is always last (fallback)
        _handlers.Add(new HandlerEntry<RichContentBlock>(new TextBlockHandler()));
    }

    internal async IAsyncEnumerable<ContentBlock> Process(
        ChatResponseUpdate update,
#pragma warning disable IDE0060 // cancellationToken reserved for future use
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        var context = new BlockMappingContext(update, _handlers);

        // Phase 1: Active entries get priority (most recent first)
        for (var i = _activeStack.Count - 1; i >= 0; i--)
        {
            if (context.AllHandled)
            {
                break;
            }

            var active = _activeStack[i];
            var result = active.Invoke(context);

            switch (result.Kind)
            {
                case HandleResult.ResultKind.Pass:
                    break;

                case HandleResult.ResultKind.Update:
                    active.Block.InvokeNotifyChanged();
                    break;

                case HandleResult.ResultKind.Complete:
                    active.Block.LifecycleState = BlockLifecycleState.Inactive;
                    active.Block.InvokeNotifyChanged();
                    _activeStack.RemoveAt(i);
                    break;
            }
        }

        // Phase 2: Inactive handlers try to claim remaining content
        if (!context.AllHandled)
        {
            for (var i = 0; i < _handlers.Count; i++)
            {
                if (context.AllHandled)
                {
                    break;
                }

                var handler = _handlers[i];

                while (!context.AllHandled)
                {
                    var progressBefore = context.HandledProgress;
                    var activeEntry = handler.TryHandle(context);
                    if (activeEntry is not null)
                    {
                        var emitBlock = activeEntry.Block;
                        emitBlock.Role = update.Role;
                        emitBlock.AuthorName = update.AuthorName;
                        emitBlock.LifecycleState = BlockLifecycleState.Active;
                        ThrowIfIdMissing(emitBlock);
                        _activeStack.Add(activeEntry);

                        yield return emitBlock;

                        // If the handler emitted without consuming any content, stop
                        // re-invoking it to prevent infinite loops.
                        if (context.HandledProgress == progressBefore)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        await Task.CompletedTask;
    }

    internal IReadOnlyList<ContentBlock> Finalize()
    {
        foreach (var active in _activeStack)
        {
            active.Block.LifecycleState = BlockLifecycleState.Inactive;
        }
        _activeStack.Clear();

        return Array.Empty<ContentBlock>();
    }

    private static void ThrowIfIdMissing(ContentBlock block)
    {
        if (string.IsNullOrEmpty(block.Id))
        {
            throw new InvalidOperationException(
                $"Block handler emitted a {block.GetType().Name} without assigning an Id.");
        }
    }
}
