// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Keeps track of the nesting of elements/containers while writing out the C# source code
    /// for a component. This allows us to detect mismatched start/end tags, as well as inject
    /// additional C# source to capture component descendants in a lambda.
    /// </summary>
    internal class ScopeStack
    {
        private readonly Stack<ScopeEntry> _stack = new Stack<ScopeEntry>();

        public void OpenScope(string tagName, bool isComponent)
        {
            _stack.Push(new ScopeEntry(tagName, isComponent));
        }

        public void CloseScope(string tagName, bool isComponent)
        {
            if (_stack.Count == 0)
            {
                throw new RazorCompilerException(
                    $"Unexpected closing tag '{tagName}' with no matching start tag.");
            }

            var expected = _stack.Pop();
            if (!tagName.Equals(expected.TagName, StringComparison.Ordinal))
            {
                throw new RazorCompilerException(
                    $"Mismatching closing tag. Found '{tagName}' but expected '{expected.TagName}'.");
            }

            // Note: there's no unit test to cover the following, because there's no known way of
            // triggering it from user code (i.e., Razor source code). But the test is here anyway
            // just in case one day it turns out there is some way of causing this error.
            if (isComponent != expected.IsComponent)
            {
                throw new RazorCompilerException(
                    $"Mismatching closing tag. Found '{tagName}' of type '{(isComponent ? "component" : "element")}' but expected type '{(expected.IsComponent ? "component" : "element")}'.");
            }
        }

        public void IncrementCurrentScopeChildCount()
        {

        }

        private class ScopeEntry
        {
            public readonly string TagName;
            public readonly bool IsComponent;
            public int ChildCount;

            public ScopeEntry(string tagName, bool isComponent)
            {
                TagName = tagName;
                IsComponent = isComponent;
                ChildCount = 0;
            }
        }
    }
}
