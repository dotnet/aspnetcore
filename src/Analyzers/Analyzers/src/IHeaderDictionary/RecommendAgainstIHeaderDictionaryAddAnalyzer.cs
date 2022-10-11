// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RecommendAgainstIHeaderDictionaryAddAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var symbols = new IHeaderDictionarySymbols(context.Compilation);

        // Don't run analyzer if ASP.NET Core types cannot be found
        if (!symbols.HasRequiredSymbols)
        {
            Debug.Fail("One or more types could not be found.");
            return;
        }

        var entryPoint = context.Compilation.GetEntryPoint(context.CancellationToken);

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (IsAddInvocation(symbols, invocation))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.RecommendAgainstIHeaderDictionaryAdd,
                        invocation.Syntax.GetLocation(),
                        invocation.Syntax.ToString()));
            }

        }, OperationKind.Invocation);
    }

    private static bool IsAddInvocation(IHeaderDictionarySymbols symbols, IInvocationOperation invocation)
    {
        if (invocation.Instance?.Type is not INamedTypeSymbol instanceTypeSymbol)
        {
            return false;
        }

        return IHeaderDictionaryFacts.IsIHeaderDictionary(symbols, instanceTypeSymbol)
            && IHeaderDictionaryFacts.IsAdd(symbols, invocation.TargetMethod);
    }
}
