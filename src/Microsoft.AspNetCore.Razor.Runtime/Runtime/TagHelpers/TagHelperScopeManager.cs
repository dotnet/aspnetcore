// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Internal;

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
        /// <param name="tagMode">HTML syntax of the element in the Razor source.</param>
        /// <param name="uniqueId">An identifier unique to the HTML element this scope is for.</param>
        /// <param name="executeChildContentAsync">A delegate used to execute the child content asynchronously.</param>
        /// <param name="startTagHelperWritingScope">
        /// A delegate used to start a writing scope in a Razor page and optionally override the page's
        /// <see cref="HtmlEncoder"/> within that scope.
        /// </param>
        /// <param name="endTagHelperWritingScope">A delegate used to end a writing scope in a Razor page.</param>
        /// <returns>A <see cref="TagHelperExecutionContext"/> to use.</returns>
        public TagHelperExecutionContext Begin(
            string tagName,
            TagMode tagMode,
            string uniqueId,
            Func<Task> executeChildContentAsync,
            Action<HtmlEncoder> startTagHelperWritingScope,
            Func<TagHelperContent> endTagHelperWritingScope)
        {
            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            if (uniqueId == null)
            {
                throw new ArgumentNullException(nameof(uniqueId));
            }

            if (executeChildContentAsync == null)
            {
                throw new ArgumentNullException(nameof(executeChildContentAsync));
            }

            if (startTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(startTagHelperWritingScope));
            }

            if (endTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(endTagHelperWritingScope));
            }

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
                tagMode,
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