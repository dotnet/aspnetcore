// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Carries a single model update through the block mapping pipeline and tracks which
/// parts of the update handlers have already claimed.
/// </summary>
public class BlockMappingContext
{
    private readonly bool[] _handled;
    private readonly IReadOnlyList<IHandlerEntry>? _handlers;
    private int _handledCount;
    private bool _updateHandled;

    internal BlockMappingContext(
        ChatResponseUpdate update,
        IReadOnlyList<IHandlerEntry>? handlers = null)
    {
        Update = update;
        _handled = new bool[update.Contents.Count];
        _handlers = handlers;
    }

    /// <summary>
    /// Gets the update being mapped.
    /// </summary>
    public ChatResponseUpdate Update { get; }

    /// <summary>
    /// Gets the contents of <see cref="Update"/> that no handler has claimed yet.
    /// </summary>
    public UnhandledContentsEnumerable UnhandledContents => new(Update.Contents, _handled);

    /// <summary>
    /// Marks a content item as claimed so that later handlers do not see it.
    /// </summary>
    /// <param name="content">The content item that was handled.</param>
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

    /// <summary>
    /// Marks the update itself as handled. Use this for updates that carry no content items.
    /// </summary>
    public void MarkUpdateHandled()
    {
        _updateHandled = true;
    }

    /// <summary>
    /// Gets a value indicating whether every part of the update has been claimed.
    /// </summary>
    public bool AllHandled =>
        _handledCount >= Update.Contents.Count && (Update.Contents.Count > 0 || _updateHandled);

    internal int HandledProgress => _handledCount + (_updateHandled ? 1 : 0);

    /// <summary>
    /// Maps content through the available handlers to create a nested block.
    /// </summary>
    /// <param name="content">The content to map.</param>
    /// <returns>The mapped block, or <see langword="null"/> when no handler accepts the content.</returns>
    public ContentBlock? CreateInnerBlock(AIContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_handlers is null)
        {
            return null;
        }

        var update = new ChatResponseUpdate
        {
            Role = Update.Role,
            AuthorName = Update.AuthorName,
            MessageId = Update.MessageId,
            Contents = [content],
        };
        var context = new BlockMappingContext(update, _handlers);

        foreach (var handler in _handlers)
        {
            var entry = handler.TryHandle(context);
            if (entry is not null)
            {
                entry.Block.Role = update.Role;
                entry.Block.AuthorName = update.AuthorName;
                entry.Block.LifecycleState = BlockLifecycleState.Inactive;
                return entry.Block;
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates the content items of an update that have not been claimed by a handler.
    /// </summary>
    public readonly struct UnhandledContentsEnumerable
    {
        private readonly IList<AIContent> _contents;
        private readonly bool[] _handled;

        internal UnhandledContentsEnumerable(IList<AIContent> contents, bool[] handled)
        {
            _contents = contents;
            _handled = handled;
        }

        /// <summary>
        /// Returns an enumerator over the unclaimed content items.
        /// </summary>
        /// <returns>An enumerator over the unclaimed content items.</returns>
        public UnhandledContentsEnumerator GetEnumerator() => new(_contents, _handled);
    }

    /// <summary>
    /// Enumerator over the content items of an update that have not been claimed by a handler.
    /// </summary>
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

        /// <summary>
        /// Gets the content item at the current position.
        /// </summary>
        public AIContent Current => _contents[_index];

        /// <summary>
        /// Advances to the next unclaimed content item.
        /// </summary>
        /// <returns><see langword="true"/> if another unclaimed item exists; otherwise <see langword="false"/>.</returns>
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
