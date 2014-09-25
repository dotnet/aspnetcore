// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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
        /// <returns>A <see cref="TagHelperExecutionContext"/> to use.</returns>
        public TagHelperExecutionContext Begin(string tagName)
        {
            var executionContext = new TagHelperExecutionContext(tagName);

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