// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.AI;

// Routes model updates to handlers in two phases:
//   1. Blocks that are still active get first refusal on the update, most recent first.
//      This keeps a streaming block accumulating instead of emitting a new block per token.
//   2. Whatever is left is offered to inactive handlers, which may emit new blocks.
internal class BlockMappingPipeline
{
    private readonly List<IHandlerEntry> _handlers = new();
    private readonly List<IActiveEntry> _activeStack = new();
    private readonly ILogger _logger;

    internal BlockMappingPipeline(UIAgentOptions options, ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;

        // User-registered handlers go first so they can customize behavior
        foreach (var registration in options.HandlerRegistrations)
        {
            _handlers.Add(registration.CreateEntry());
        }

        if (options.UIActions.Count > 0)
        {
            _handlers.Add(new HandlerEntry<UIActionHandler.State>(
                new UIActionHandler(options.UIActions)));
        }

        _handlers.Add(new HandlerEntry<FunctionApprovalHandler.State>(
            new FunctionApprovalHandler()));

        _handlers.Add(new HandlerEntry<FunctionInvocationContentBlock>(
            new FunctionInvocationHandler()));

        // Structured snapshots take precedence over the plain-text fallback.
        _handlers.Add(new HandlerEntry<RichContentBlock>(new RichTextContentHandler()));

        // Built-in plain-text handler is always last (fallback).
        _handlers.Add(new HandlerEntry<RichContentBlock>(new TextBlockHandler()));
    }

    internal async IAsyncEnumerable<ContentBlock> Process(
        ChatResponseUpdate update,
#pragma warning disable IDE0060 // cancellationToken reserved for future use
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
    {
        var contentTypes = string.Join(", ", update.Contents.Select(c => c.GetType().Name));
        BlockMappingPipelineLog.ProcessingUpdate(_logger, update.Role?.Value, update.Contents.Count, contentTypes);

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

            BlockMappingPipelineLog.ActiveHandlerResult(_logger, active.Block.GetType().Name, result.Kind.ToString(), active.Block.Id);

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
                BlockMappingPipelineLog.TryingInactiveHandler(_logger, handler.GetType().Name);

                while (!context.AllHandled)
                {
                    var progressBefore = context.HandledProgress;
                    var activeEntry = handler.TryHandle(context);
                    if (activeEntry is null)
                    {
                        break;
                    }

                    var emitBlock = activeEntry.Block;
                    emitBlock.Role = update.Role;
                    emitBlock.AuthorName = update.AuthorName;
                    emitBlock.LifecycleState = BlockLifecycleState.Active;
                    ThrowIfIdMissing(emitBlock);

                    var keepActive = true;
                    if (emitBlock is FunctionInvocationContentBlock)
                    {
                        var catchUpResult = activeEntry.Invoke(context);

                        BlockMappingPipelineLog.ActiveHandlerResult(_logger, emitBlock.GetType().Name, catchUpResult.Kind.ToString(), emitBlock.Id);

                        if (catchUpResult.Kind == HandleResult.ResultKind.Complete)
                        {
                            emitBlock.LifecycleState = BlockLifecycleState.Inactive;
                            keepActive = false;
                        }
                    }

                    if (keepActive)
                    {
                        _activeStack.Add(activeEntry);
                    }

                    BlockMappingPipelineLog.EmittingBlock(_logger, emitBlock.GetType().Name, emitBlock.Id, emitBlock.Role?.Value);

                    yield return emitBlock;

                    // If the handler emitted without consuming any content, stop
                    // re-invoking it to prevent infinite loops.
                    if (context.HandledProgress == progressBefore)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            BlockMappingPipelineLog.AllContentHandledAfterPhase1(_logger);
        }

        await Task.CompletedTask;
    }

    internal IReadOnlyList<ContentBlock> Finalize()
    {
        BlockMappingPipelineLog.Finalizing(_logger, _activeStack.Count);

        foreach (var active in _activeStack)
        {
            active.Block.LifecycleState = BlockLifecycleState.Inactive;
            active.Block.InvokeNotifyChanged();
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
