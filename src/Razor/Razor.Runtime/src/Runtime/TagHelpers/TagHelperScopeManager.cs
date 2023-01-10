// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

/// <summary>
/// Class that manages <see cref="TagHelperExecutionContext"/> scopes.
/// </summary>
public class TagHelperScopeManager
{
    private readonly ExecutionContextPool _executionContextPool;

    /// <summary>
    /// Instantiates a new <see cref="TagHelperScopeManager"/>.
    /// </summary>
    /// <param name="startTagHelperWritingScope">
    /// A delegate used to start a writing scope in a Razor page and optionally override the page's
    /// <see cref="HtmlEncoder"/> within that scope.
    /// </param>
    /// <param name="endTagHelperWritingScope">A delegate used to end a writing scope in a Razor page.</param>
    public TagHelperScopeManager(
        Action<HtmlEncoder> startTagHelperWritingScope,
        Func<TagHelperContent> endTagHelperWritingScope)
    {
        ArgumentNullException.ThrowIfNull(startTagHelperWritingScope);
        ArgumentNullException.ThrowIfNull(endTagHelperWritingScope);

        _executionContextPool = new ExecutionContextPool(startTagHelperWritingScope, endTagHelperWritingScope);
    }

    /// <summary>
    /// Starts a <see cref="TagHelperExecutionContext"/> scope.
    /// </summary>
    /// <param name="tagName">The HTML tag name that the scope is associated with.</param>
    /// <param name="tagMode">HTML syntax of the element in the Razor source.</param>
    /// <param name="uniqueId">An identifier unique to the HTML element this scope is for.</param>
    /// <param name="executeChildContentAsync">A delegate used to execute the child content asynchronously.</param>
    /// <returns>A <see cref="TagHelperExecutionContext"/> to use.</returns>
    public TagHelperExecutionContext Begin(
        string tagName,
        TagMode tagMode,
        string uniqueId,
        Func<Task> executeChildContentAsync)
    {
        ArgumentNullException.ThrowIfNull(tagName);
        ArgumentNullException.ThrowIfNull(uniqueId);
        ArgumentNullException.ThrowIfNull(executeChildContentAsync);

        IDictionary<object, object> items;
        var parentExecutionContext = _executionContextPool.Current;

        // If we're not wrapped by another TagHelper, then there will not be a parentExecutionContext.
        if (parentExecutionContext != null)
        {
            items = new CopyOnWriteDictionary<object, object>(
                parentExecutionContext.Items,
                comparer: EqualityComparer<object>.Default);
        }
        else
        {
            items = new Dictionary<object, object>();
        }

        var executionContext = _executionContextPool.Rent(
            tagName,
            tagMode,
            items,
            uniqueId,
            executeChildContentAsync);

        return executionContext;
    }

    /// <summary>
    /// Ends a <see cref="TagHelperExecutionContext"/> scope.
    /// </summary>
    /// <returns>If the current scope is nested, the parent <see cref="TagHelperExecutionContext"/>.
    /// <c>null</c> otherwise.</returns>
    public TagHelperExecutionContext End()
    {
        if (_executionContextPool.Current == null)
        {
            throw new InvalidOperationException(
                Resources.FormatScopeManager_EndCannotBeCalledWithoutACallToBegin(
                    nameof(End),
                    nameof(Begin),
                    nameof(TagHelperScopeManager)));
        }

        _executionContextPool.ReturnCurrent();

        var parentExecutionContext = _executionContextPool.Current;

        return parentExecutionContext;
    }

    private sealed class ExecutionContextPool
    {
        private readonly Action<HtmlEncoder> _startTagHelperWritingScope;
        private readonly Func<TagHelperContent> _endTagHelperWritingScope;
        private readonly List<TagHelperExecutionContext> _executionContexts;
        private int _nextIndex;

        public ExecutionContextPool(
            Action<HtmlEncoder> startTagHelperWritingScope,
            Func<TagHelperContent> endTagHelperWritingScope)
        {
            _executionContexts = new List<TagHelperExecutionContext>();
            _startTagHelperWritingScope = startTagHelperWritingScope;
            _endTagHelperWritingScope = endTagHelperWritingScope;
        }

        public TagHelperExecutionContext Current => _nextIndex > 0 ? _executionContexts[_nextIndex - 1] : null;

        public TagHelperExecutionContext Rent(
            string tagName,
            TagMode tagMode,
            IDictionary<object, object> items,
            string uniqueId,
            Func<Task> executeChildContentAsync)
        {
            TagHelperExecutionContext tagHelperExecutionContext;

            if (_nextIndex == _executionContexts.Count)
            {
                tagHelperExecutionContext = new TagHelperExecutionContext(
                    tagName,
                    tagMode,
                    items,
                    uniqueId,
                    executeChildContentAsync,
                    _startTagHelperWritingScope,
                    _endTagHelperWritingScope);

                _executionContexts.Add(tagHelperExecutionContext);
            }
            else
            {
                tagHelperExecutionContext = _executionContexts[_nextIndex];
                tagHelperExecutionContext.Reinitialize(tagName, tagMode, items, uniqueId, executeChildContentAsync);
            }

            _nextIndex++;

            return tagHelperExecutionContext;
        }

        public void ReturnCurrent() => _nextIndex--;
    }
}
