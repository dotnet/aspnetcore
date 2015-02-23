// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class that manages <see cref="TagHelperExecutionContext"/> scopes.
    /// </summary>
    public class TagHelperScopeManager
    {
        private readonly Stack<TagHelperExecutionContext> _executionScopes;

        /// <summary>
        /// Instantiates a new <see cref="TagHelperScopeManager"/>.
        /// </summary>
        public TagHelperScopeManager()
        {
            _executionScopes = new Stack<TagHelperExecutionContext>();
        }

        /// <summary>
        /// Starts a <see cref="TagHelperExecutionContext"/> scope.
        /// </summary>
        /// <param name="tagName">The HTML tag name that the scope is associated with.</param>
        /// <param name="selfClosing">
        /// <see cref="bool"/> indicating whether or not the tag of this scope is self-closing.
        /// </param>
        /// <param name="uniqueId">An identifier unique to the HTML element this scope is for.</param>
        /// <param name="executeChildContentAsync">A delegate used to execute the child content asynchronously.</param>
        /// <param name="startTagHelperWritingScope">A delegate used to start a writing scope in a Razor page.</param>
        /// <param name="endTagHelperWritingScope">A delegate used to end a writing scope in a Razor page.</param>
        /// <returns>A <see cref="TagHelperExecutionContext"/> to use.</returns>
        public TagHelperExecutionContext Begin(
            [NotNull] string tagName,
            bool selfClosing,
            [NotNull] string uniqueId,
            [NotNull] Func<Task> executeChildContentAsync,
            [NotNull] Action startTagHelperWritingScope,
            [NotNull] Func<TagHelperContent> endTagHelperWritingScope)
        {
            IDictionary<object, object> items;

            // If we're not wrapped by another TagHelper, then there will not be a parentExecutionContext.
            if (_executionScopes.Count > 0)
            {
                items = new CopyOnWriteDictionary<object, object>(
                    _executionScopes.Peek().Items,
                    comparer: EqualityComparer<object>.Default);
            }
            else
            {
                items = new Dictionary<object, object>();
            }

            var executionContext = new TagHelperExecutionContext(
                tagName,
                selfClosing,
                items,
                uniqueId,
                executeChildContentAsync,
                startTagHelperWritingScope,
                endTagHelperWritingScope);

            _executionScopes.Push(executionContext);

            return executionContext;
        }

        /// <summary>
        /// Ends a <see cref="TagHelperExecutionContext"/> scope.
        /// </summary>
        /// <returns>If the current scope is nested, the parent <see cref="TagHelperExecutionContext"/>.
        /// <c>null</c> otherwise.</returns>
        public TagHelperExecutionContext End()
        {
            if (_executionScopes.Count == 0)
            {
                throw new InvalidOperationException(
                    Resources.FormatScopeManager_EndCannotBeCalledWithoutACallToBegin(
                        nameof(End),
                        nameof(Begin),
                        nameof(TagHelperScopeManager)));
            }

            _executionScopes.Pop();

            if (_executionScopes.Count != 0)
            {
                return _executionScopes.Peek();
            }

            return null;
        }
    }
}