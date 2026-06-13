// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class BlockMappingContext
{
    private readonly bool[] _handled;
    private int _handledCount;
    private bool _updateHandled;
    private readonly IReadOnlyList<IHandlerEntry>? _inactiveHandlers;

    internal BlockMappingContext(ChatResponseUpdate update)
        : this(update, inactiveHandlers: null)
    {
    }

    internal BlockMappingContext(
        ChatResponseUpdate update,
        IReadOnlyList<IHandlerEntry>? inactiveHandlers)
    {
        Update = update;
        _handled = new bool[update.Contents.Count];
        _inactiveHandlers = inactiveHandlers;
    }

    public ChatResponseUpdate Update { get; }

    public UnhandledContentsEnumerable UnhandledContents => new(Update.Contents, _handled);

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

    public void MarkUpdateHandled()
    {
        _updateHandled = true;
    }

    public bool AllHandled =>
        _handledCount >= Update.Contents.Count && (Update.Contents.Count > 0 || _updateHandled);

    internal int HandledProgress => _handledCount + (_updateHandled ? 1 : 0);

    public ContentBlock? CreateInnerBlock(AIContent content)
    {
        if (_inactiveHandlers is null)
        {
            return null;
        }

        var tempUpdate = new ChatResponseUpdate { Contents = [content] };
        var tempContext = new BlockMappingContext(tempUpdate);

        for (var i = 0; i < _inactiveHandlers.Count; i++)
        {
            var activeEntry = _inactiveHandlers[i].TryHandle(tempContext);
            if (activeEntry is not null)
            {
                return activeEntry.Block;
            }
        }

        return null;
    }

    public readonly struct UnhandledContentsEnumerable
    {
        private readonly IList<AIContent> _contents;
        private readonly bool[] _handled;

        internal UnhandledContentsEnumerable(IList<AIContent> contents, bool[] handled)
        {
            _contents = contents;
            _handled = handled;
        }

        public UnhandledContentsEnumerator GetEnumerator() => new(_contents, _handled);
    }

    public struct UnhandledContentsEnumerator
    {
        private readonly IList<AIContent> _contents;
        private readonly bool[] _handled;
        private int _index;

        internal UnhandledContentsEnumerator(IList<AIContent> contents, bool[] handled)
        {
            _contents = contents;
            _handled = handled;
            _index = -1;
        }

        public AIContent Current => _contents[_index];

        public bool MoveNext()
        {
            while (++_index < _contents.Count)
            {
                if (!_handled[_index])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
