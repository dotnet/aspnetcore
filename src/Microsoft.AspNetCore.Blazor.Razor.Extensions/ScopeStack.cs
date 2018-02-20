// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
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
        private int _builderVarNumber = 1;

        public string BuilderVarName { get; private set; } = "builder";

        public void OpenScope(string tagName, bool isComponent)
        {
            _stack.Push(new ScopeEntry(tagName, isComponent));
        }

        public void CloseScope(CodeRenderingContext context, string tagName, bool isComponent, SourceSpan? source)
        {
            if (_stack.Count == 0)
            {
                throw new RazorCompilerException(
                    $"Unexpected closing tag '{tagName}' with no matching start tag.", source);
            }

            var currentScope = _stack.Pop();
            if (!tagName.Equals(currentScope.TagName, StringComparison.Ordinal))
            {
                throw new RazorCompilerException(
                    $"Mismatching closing tag. Found '{tagName}' but expected '{currentScope.TagName}'.", source);
            }

            // Note: there's no unit test to cover the following, because there's no known way of
            // triggering it from user code (i.e., Razor source code). But the check is here anyway
            // just in case one day it turns out there is some way of causing this error.
            if (isComponent != currentScope.IsComponent)
            {
                throw new RazorCompilerException(
                    $"Mismatching closing tag. Found '{tagName}' of type '{(isComponent ? "component" : "element")}' but expected type '{(currentScope.IsComponent ? "component" : "element")}'.", source);
            }

            // When closing the scope for a component with children, it's time to close the lambda
            if (currentScope.LambdaScope != null)
            {
                currentScope.LambdaScope.Dispose();
                context.CodeWriter.Write(")");
                context.CodeWriter.WriteEndMethodInvocation();
                OffsetBuilderVarNumber(-1);
            }
        }

        public void IncrementCurrentScopeChildCount(CodeRenderingContext context)
        {
            if (_stack.Count > 0)
            {
                var currentScope = _stack.Peek();

                if (currentScope.IsComponent && currentScope.ChildCount == 0)
                {
                    // When we're about to insert the first child into a component,
                    // it's time to open a new lambda
                    var blazorNodeWriter = (BlazorIntermediateNodeWriter)context.NodeWriter;
                    blazorNodeWriter.BeginWriteAttribute(context.CodeWriter, BlazorApi.RenderTreeBuilder.ChildContent);
                    OffsetBuilderVarNumber(1);
                    context.CodeWriter.Write($"({BlazorApi.RenderFragment.FullTypeName})(");
                    currentScope.LambdaScope = context.CodeWriter.BuildLambda(BuilderVarName);
                }

                currentScope.ChildCount++;
            }
        }

        private void OffsetBuilderVarNumber(int delta)
        {
            _builderVarNumber += delta;
            BuilderVarName = _builderVarNumber == 1
                ? "builder"
                : $"builder{_builderVarNumber}";
        }

        private class ScopeEntry
        {
            public readonly string TagName;
            public readonly bool IsComponent;
            public int ChildCount;
            public IDisposable LambdaScope;

            public ScopeEntry(string tagName, bool isComponent)
            {
                TagName = tagName;
                IsComponent = isComponent;
                ChildCount = 0;
            }
        }
    }
}
