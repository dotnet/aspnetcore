// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Http;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class HeaderDictionaryAddAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (invocation.Instance?.Type is INamedTypeSymbol type &&
                IsIHeadersDictionaryType(type))
            {
                if (invocation.TargetMethod.Parameters.Length == 2 &&
                    IsAddMethod(invocation.TargetMethod))
                {
                    AddDiagnosticWarning(context, invocation.Syntax.GetLocation());
                }
            }
        }, OperationKind.Invocation);
    }

    private static bool IsIHeadersDictionaryType(INamedTypeSymbol type)
    {
        // Only IHeaderDictionary is valid. Types like HeaderDictionary, which implement IHeaderDictionary,
        // can't access header properties unless cast as IHeaderDictionary.
        return type is
        {
            Name: "IHeaderDictionary",
            ContainingNamespace:
            {
                Name: "Http",
                ContainingNamespace:
                {
                    Name: "AspNetCore",
                    ContainingNamespace:
                    {
                        Name: "Microsoft",
                        ContainingNamespace:
                        {
                            IsGlobalNamespace: true
                        }
                    }
                }
            }
        };
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
