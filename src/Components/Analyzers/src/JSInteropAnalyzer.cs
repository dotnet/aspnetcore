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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnguardedJSInteropCall);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolStartAction(context =>
        {
            var unguardedJSInterlopCalls = new ConcurrentDictionary<IMethodSymbol, ImmutableArray<Location>>(SymbolEqualityComparer.Default);

            //collect JSInterop calls
            context.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;

                // Get the symbol being invoked
                var symbol = invocation.TargetMethod;

                if (symbol == null) {
                    return;
                }

                if (symbol.ContainingType.Name == "JSRuntimeExtensions" &&
                 !invocation.Syntax.Ancestors().OfType<TryStatementSyntax>().Any()
                )
                {
                    // This is a JsInterop call not within a try block
                    unguardedJSInterlopCalls.TryAdd(symbol, ImmutableArray.Create(invocation.Syntax.GetLocation()));
                }

            }, OperationKind.Invocation);

            context.RegisterSymbolEndAction(endContext =>
            {
                foreach (var call in unguardedJSInterlopCalls)
                {
                    var method = call.Key;
                    var locations = call.Value;
                    foreach (var location in locations)
                    {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.UnguardedJSInteropCall,
                            location,
                            method.Name));
                    }
                }
            });
        }, SymbolKind.NamedType);
    }
}
