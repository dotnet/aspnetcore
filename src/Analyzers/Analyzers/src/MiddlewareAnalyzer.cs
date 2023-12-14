// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class MiddlewareAnalyzer
{
    private readonly StartupAnalysisBuilder _context;

    public MiddlewareAnalyzer(StartupAnalysisBuilder context)
    {
        _context = context;
    }

    public void AnalyzeConfigureMethod(OperationBlockStartAnalysisContext context)
    {
        var configureMethod = (IMethodSymbol)context.OwningSymbol;
        var middleware = ImmutableArray.CreateBuilder<MiddlewareItem>();

        // Note: this is a simple source-order implementation. We don't attempt perform data flow
        // analysis in order to determine the actual order in which middleware are ordered.
        //
        // This can currently be confused by things like Map(...)
        context.RegisterOperationAction(context =>
        {
            // We're looking for usage of extension methods, so we need to look at the 'this' parameter
            // rather than invocation.Instance.
            if (context.Operation is IInvocationOperation invocation &&
            invocation.Instance == null &&
            invocation.Arguments.Length >= 1 &&
            SymbolEqualityComparer.Default.Equals(invocation.Arguments[0].Parameter?.Type, _context.StartupSymbols.IApplicationBuilder))
            {
                middleware.Add(new MiddlewareItem(invocation));
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(context =>
        {
            _context.ReportAnalysis(new MiddlewareAnalysis(configureMethod, middleware.ToImmutable()));
        });
    }
}
