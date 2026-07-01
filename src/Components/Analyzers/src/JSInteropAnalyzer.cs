// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInteropAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> JSInteropTypeNames = ImmutableHashSet.Create(
        "JSRuntimeExtensions",
        "JSObjectReferenceExtensions",
        "IJSRuntime",
        "IJSInProcessRuntime",
        "IJSObjectReference",
        "IJSInProcessObjectReference");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnguardedJSInteropCall);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolStartAction(context =>
        {
            var unguardedJSInteropCalls = new ConcurrentBag<(IMethodSymbol Method, Location Location)>();

            context.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                var symbol = invocation.TargetMethod;

                if (symbol is null)
                {
                    return;
                }

                if (JSInteropTypeNames.Contains(symbol.ContainingType.Name) &&
                    !invocation.Syntax.Ancestors().OfType<TryStatementSyntax>().Any())
                {
                    unguardedJSInteropCalls.Add((symbol, invocation.Syntax.GetLocation()));
                }
            }, OperationKind.Invocation);

            context.RegisterSymbolEndAction(endContext =>
            {
                foreach (var (method, location) in unguardedJSInteropCalls)
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnguardedJSInteropCall,
                        location,
                        method.Name));
                }
            });
        }, SymbolKind.NamedType);
    }
}
