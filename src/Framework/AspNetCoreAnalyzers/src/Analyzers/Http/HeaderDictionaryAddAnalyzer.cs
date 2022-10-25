// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Http;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HeaderDictionaryAddAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var symbols = new HeaderDictionarySymbols(context.Compilation);

        if (!symbols.HasRequiredSymbols)
        {
            return;
        }

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (SymbolEqualityComparer.Default.Equals(symbols.IHeaderDictionary, invocation.Instance?.Type)
                && IsAddMethod(invocation.TargetMethod)
                && invocation.TargetMethod.Parameters.Length == 2)
            {
                AddDiagnosticWarning(context, invocation.Syntax.GetLocation());
            }

        }, OperationKind.Invocation);
    }

    private static bool IsAddMethod(IMethodSymbol method)
    {
        return method is
        {
            Name: "Add",
            ContainingType:
            {
                Name: "IDictionary",
                ContainingNamespace:
                {
                    Name: "Generic",
                    ContainingNamespace:
                    {
                        Name: "Collections",
                        ContainingNamespace:
                        {
                            Name: "System",
                            ContainingNamespace:
                            {
                                IsGlobalNamespace: true
                            }
                        }
                    }
                }
            }
        };
    }

    private static void AddDiagnosticWarning(OperationAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd,
            location));
    }

    private sealed class HeaderDictionarySymbols
    {
        public HeaderDictionarySymbols(Compilation compilation)
        {
            IHeaderDictionary = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IHeaderDictionary");
        }

        public bool HasRequiredSymbols => IHeaderDictionary is not null;

        public INamedTypeSymbol IHeaderDictionary { get; }
    }
}
