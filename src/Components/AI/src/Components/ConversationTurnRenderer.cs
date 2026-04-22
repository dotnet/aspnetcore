// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

internal class ConversationTurnRenderer : IDisposable
{
    private readonly ConversationTurn _turn;
    private readonly MessageListContext _listContext;
    private readonly Action _requestRender;
    private readonly List<BlockContainer> _blockContainers = new();
    private readonly IDisposable? _blockAddedSub;

    internal ConversationTurnRenderer(
        AgentContext agentContext,
        ConversationTurn turn,
        MessageListContext listContext,
        Action requestRender)
    {
        _turn = turn;
        _listContext = listContext;
        _requestRender = requestRender;

        _blockAddedSub = agentContext.RegisterOnBlockAdded((t, block) =>
        {
            if (ReferenceEquals(t, _turn))
            {
                OnBlockAdded(block);
            }
        });

        foreach (var block in turn.RequestBlocks)
        {
            _blockContainers.Add(new BlockContainer(block, _listContext, _requestRender));
        }
        foreach (var block in turn.ResponseBlocks)
        {
            _blockContainers.Add(new BlockContainer(block, _listContext, _requestRender));
        }
    }

    private void OnBlockAdded(ContentBlock block)
    {
        _blockContainers.Add(new BlockContainer(block, _listContext, _requestRender));
        _requestRender();
    }

    internal void RenderTo(RenderTreeBuilder builder, int baseSeq)
    {
        // Determine role from request blocks (user turn) or response blocks
        var role = _turn.RequestBlocks.Count > 0 ? "user" : "assistant";
        builder.OpenElement(baseSeq, "div");
        builder.AddAttribute(baseSeq + 1, "class", $"sc-ai-turn sc-ai-turn--{role}");
        var seq = baseSeq + 10;
        foreach (var container in _blockContainers)
        {
            container.RenderTo(builder, seq);
            seq += 10;
        }
        builder.CloseElement();
    }

    public void Dispose()
    {
        _blockAddedSub?.Dispose();
        foreach (var container in _blockContainers)
        {
            container.Dispose();
        }
    }
}
