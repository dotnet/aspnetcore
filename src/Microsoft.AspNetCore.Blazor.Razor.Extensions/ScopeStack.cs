// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Shared;
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

        public void OpenElementScope(string tagName)
        {
            _stack.Push(new ScopeEntry(tagName, ScopeKind.Element));
        }

        public void OpenComponentScope(string tagName)
        {
            _stack.Push(new ScopeEntry(tagName, ScopeKind.Component));
        }

        public void OpenTemplateScope(CodeRenderingContext context, string variableName)
        {
            var currentScope = new ScopeEntry("__template", ScopeKind.Template);
            _stack.Push(currentScope);

            // Templates always get a lambda scope, because they are defined as a lambda.
            OffsetBuilderVarNumber(1);
            currentScope.LambdaScope = context.CodeWriter.BuildLambda(BuilderVarName, variableName);

        }

        public void CloseScope(CodeRenderingContext context)
        {
            var currentScope = _stack.Pop();

            // When closing the scope for a component with children, it's time to close the lambda
            if (currentScope.LambdaScope != null)
            {
                currentScope.LambdaScope.Dispose();

                if (currentScope.Kind == ScopeKind.Component)
                {
                    context.CodeWriter.Write(")");
                    context.CodeWriter.WriteEndMethodInvocation();
                }

                OffsetBuilderVarNumber(-1);
            }
        }

        public void IncrementCurrentScopeChildCount(CodeRenderingContext context)
        {
            if (_stack.Count > 0)
            {
                var currentScope = _stack.Peek();

                if (currentScope.Kind == ScopeKind.Component && currentScope.ChildCount == 0)
                {
                    // When we're about to insert the first child into a component,
                    // it's time to open a new lambda
                    var blazorNodeWriter = (BlazorNodeWriter)context.NodeWriter;
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
            public ScopeKind Kind;
            public int ChildCount;
            public IDisposable LambdaScope;

            public ScopeEntry(string tagName, ScopeKind kind)
            {
                TagName = tagName;
                Kind = kind;
                ChildCount = 0;
            }

            public override string ToString() => $"<{TagName}> ({Kind})";
        }

        private enum ScopeKind
        {
            Element,
            Component,
            Template,
        }
    }
}
