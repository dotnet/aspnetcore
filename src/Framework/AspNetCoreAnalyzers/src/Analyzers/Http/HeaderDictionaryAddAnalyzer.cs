// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Http;

using WellKnownType = WellKnownTypeData.WellKnownType;

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
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.Compilation);

        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (IsAddMethod(invocation.TargetMethod)
                && invocation.TargetMethod.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IHeaderDictionary), invocation.Instance?.Type))
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
}
