// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Carries a model update through typed state mapping and tracks content consumed as state.
/// </summary>
public class StateMapperContext
{
    private readonly bool[] _handled;
    private int _handledCount;

    internal StateMapperContext(ChatResponseUpdate update)
    {
        Update = update;
        _handled = new bool[update.Contents.Count];
    }

    /// <summary>
    /// Gets the update being mapped.
    /// </summary>
    public ChatResponseUpdate Update { get; }

    /// <summary>
    /// Gets content items that have not been consumed by the state mapper.
    /// </summary>
    public IEnumerable<AIContent> UnhandledContents
    {
        get
        {
            var contents = Update.Contents;
            for (var i = 0; i < contents.Count; i++)
            {
                if (!_handled[i])
                {
                    yield return contents[i];
                }
            }
        }
    }

    /// <summary>
    /// Marks a content item as consumed by the state mapper.
    /// </summary>
    /// <param name="content">The content item that was handled.</param>
    public void MarkHandled(AIContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var contents = Update.Contents;
        for (var i = 0; i < contents.Count; i++)
        {
            if (ReferenceEquals(contents[i], content))
            {
                if (!_handled[i])
                {
                    _handled[i] = true;
                    _handledCount++;
                }

                return;
            }
        }
    }

    internal object? StateValue { get; private set; }

    /// <summary>
    /// Sets the next typed state value.
    /// </summary>
    /// <param name="value">The next state value.</param>
    public void SetState(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        StateValue = value;
    }

    internal bool HasHandledContent => _handledCount > 0;

    internal ChatResponseUpdate GetFilteredUpdate()
    {
        if (_handledCount == 0)
        {
            return Update;
        }

        var filtered = new List<AIContent>();
        var contents = Update.Contents;
        for (var i = 0; i < contents.Count; i++)
        {
            if (!_handled[i])
            {
                filtered.Add(contents[i]);
            }
        }

        return new ChatResponseUpdate
        {
            Role = Update.Role,
            AuthorName = Update.AuthorName,
            MessageId = Update.MessageId,
            ResponseId = Update.ResponseId,
            ConversationId = Update.ConversationId,
            CreatedAt = Update.CreatedAt,
            FinishReason = Update.FinishReason,
            ModelId = Update.ModelId,
            ContinuationToken = Update.ContinuationToken,
            RawRepresentation = Update.RawRepresentation,
            AdditionalProperties = Update.AdditionalProperties,
            Contents = filtered,
        };
    }
}
