// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// A single exchange in a conversation: the blocks produced by the user's message and the
/// blocks produced by the response to it.
/// </summary>
public class ConversationTurn
{
    private readonly List<ContentBlock> _requestBlocks = new();
    private readonly List<ContentBlock> _responseBlocks = new();

    /// <summary>
    /// Gets the identifier of this turn.
    /// </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the blocks produced by the message that started this turn.
    /// </summary>
    public IReadOnlyList<ContentBlock> RequestBlocks => _requestBlocks;

    /// <summary>
    /// Gets the blocks produced by the response to this turn.
    /// </summary>
    public IReadOnlyList<ContentBlock> ResponseBlocks => _responseBlocks;

    internal void AddRequestBlock(ContentBlock block)
    {
        _requestBlocks.Add(block);
    }

    internal void AddResponseBlock(ContentBlock block)
    {
        _responseBlocks.Add(block);
    }

    internal void ClearResponseBlocks()
    {
        _responseBlocks.Clear();
    }
}
