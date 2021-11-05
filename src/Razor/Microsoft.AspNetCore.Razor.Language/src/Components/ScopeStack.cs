// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Keeps track of the nesting of elements/containers while writing out the C# source code
/// for a component. This allows us to detect mismatched start/end tags, as well as inject
/// additional C# source to capture component descendants in a lambda.
/// </summary>
internal class ScopeStack
{
    private readonly Stack<ScopeEntry> _stack = new Stack<ScopeEntry>();
    private int _builderVarNumber = 1;

    public string BuilderVarName { get; private set; } = ComponentsApi.RenderTreeBuilder.BuilderParameter;

    public int Depth => _stack.Count;

    public void OpenComponentScope(CodeRenderingContext context, string name, string parameterName)
    {
        var scope = new ScopeEntry(name, ScopeKind.Component);
        _stack.Push(scope);

        OffsetBuilderVarNumber(1);

        // Writes code that looks like:
        //
        // ((__builder) => { ... })
        // OR
        // ((context) => (__builder) => { ... })

        if (parameterName != null)
        {
            context.CodeWriter.Write($"({parameterName}) => ");
        }

        scope.LambdaScope = context.CodeWriter.BuildLambda(BuilderVarName);
    }

    public void OpenTemplateScope(CodeRenderingContext context)
    {
        var currentScope = new ScopeEntry("__template", ScopeKind.Template);
        _stack.Push(currentScope);

        // Templates always get a lambda scope, because they are defined as a lambda.
        OffsetBuilderVarNumber(1);
        currentScope.LambdaScope = context.CodeWriter.BuildLambda(BuilderVarName);
    }

    public void CloseScope(CodeRenderingContext context)
    {
        var currentScope = _stack.Pop();
        currentScope.LambdaScope.Dispose();
        OffsetBuilderVarNumber(-1);
    }

    private void OffsetBuilderVarNumber(int delta)
    {
        _builderVarNumber += delta;
        BuilderVarName = _builderVarNumber == 1
            ? ComponentsApi.RenderTreeBuilder.BuilderParameter
            : $"{ComponentsApi.RenderTreeBuilder.BuilderParameter}{_builderVarNumber}";
    }

    private class ScopeEntry
    {
        public readonly string Name;
        public ScopeKind Kind;
        public int ChildCount;
        public IDisposable LambdaScope;

        public ScopeEntry(string name, ScopeKind kind)
        {
            Name = name;
            Kind = kind;
            ChildCount = 0;
        }

        public override string ToString() => $"<{Name}> ({Kind})";
    }

    private enum ScopeKind
    {
        Component,
        Template,
    }
}
