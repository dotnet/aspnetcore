// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class StateMapperContext
{
    private readonly bool[] _handled;
    private int _handledCount;

    internal StateMapperContext(ChatResponseUpdate update)
    {
        Update = update;
        _handled = new bool[update.Contents.Count];
    }

    public ChatResponseUpdate Update { get; }

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

    public void MarkHandled(AIContent content)
    {
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

    public object? StateValue { get; private set; }

    public void SetState(object value)
    {
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
            FinishReason = Update.FinishReason,
            Contents = filtered,
        };
    }
}
