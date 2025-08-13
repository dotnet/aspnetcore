// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ElementReferenceUsageAnalyzer : DiagnosticAnalyzer
{
    public ElementReferenceUsageAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
            {
                // Types we need are not defined.
                return;
            }

            if (symbols.ElementReferenceType == null)
            {
                // ElementReference type not available.
                return;
            }

            context.RegisterOperationAction(operationContext =>
            {
                AnalyzeElementReferenceUsage(operationContext, symbols);
            }, 
            OperationKind.FieldReference, 
            OperationKind.PropertyReference);
        });
    }

    private static void AnalyzeElementReferenceUsage(OperationAnalysisContext context, ComponentSymbols symbols)
    {
        var memberReference = context.Operation as IMemberReferenceOperation;
        if (memberReference == null)
        {
            return;
        }

        var memberSymbol = memberReference.Member;
        var memberType = GetMemberType(memberSymbol);
        
        // Check if the member is of ElementReference type
        if (memberType == null || !IsElementReferenceType(memberType, symbols))
        {
            return;
        }

        // Check if we're in an OnAfterRenderAsync or OnAfterRender method
        if (IsInOnAfterRenderMethod(context.Operation))
        {
            return;
        }

        // Report diagnostic for ElementReference usage outside of OnAfterRenderAsync
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync,
            memberReference.Syntax.GetLocation(),
            memberSymbol.Name));
    }

    private static bool IsElementReferenceType(ITypeSymbol type, ComponentSymbols symbols)
    {
        return SymbolEqualityComparer.Default.Equals(type, symbols.ElementReferenceType);
    }

    private static ITypeSymbol GetMemberType(ISymbol memberSymbol)
    {
        return memberSymbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };
    }

    private static bool IsInOnAfterRenderMethod(IOperation operation)
    {
        // Walk up the operation tree to find the containing method
        var current = operation;
        while (current != null)
        {
            // Look for the method symbol in different contexts
            if (current.SemanticModel != null)
            {
                var symbol = current.SemanticModel.GetEnclosingSymbol(current.Syntax.SpanStart);
                if (symbol is IMethodSymbol methodSymbol)
                {
                    return methodSymbol.Name == "OnAfterRenderAsync" || methodSymbol.Name == "OnAfterRender";
                }
            }
            
            current = current.Parent;
        }

        return false;
    }
}