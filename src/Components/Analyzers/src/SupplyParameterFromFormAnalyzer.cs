// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SupplyParameterFromFormAnalyzer : DiagnosticAnalyzer
{
    public SupplyParameterFromFormAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.SupplyParameterFromFormShouldNotHavePropertyInitializer);
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

            context.RegisterSyntaxNodeAction(context =>
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

                // Check if property has an initializer
                if (propertyDeclaration.Initializer == null)
                {
                    return;
                }

                // Ignore initializers that set to default values (null, default, etc.)
                if (IsDefaultValueInitializer(propertyDeclaration.Initializer.Value))
                {
                    return;
                }

                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
                if (propertySymbol == null)
                {
                    return;
                }

                // Check if property has [SupplyParameterFromForm] attribute
                if (!ComponentFacts.IsSupplyParameterFromForm(symbols, propertySymbol))
                {
                    return;
                }

                // Check if the containing type inherits from ComponentBase
                var containingType = propertySymbol.ContainingType;
                if (!ComponentFacts.IsComponentBase(symbols, containingType))
                {
                    return;
                }

                var propertyLocation = propertySymbol.Locations.FirstOrDefault();
                if (propertyLocation != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.SupplyParameterFromFormShouldNotHavePropertyInitializer,
                        propertyLocation,
                        propertySymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }, SyntaxKind.PropertyDeclaration);
        });
    }

    private static bool IsDefaultValueInitializer(ExpressionSyntax expression)
    {
        return expression switch
        {
            // null
            LiteralExpressionSyntax { Token.ValueText: "null" } => true,
            // null!
            PostfixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax { Token.ValueText: "null" }, OperatorToken.ValueText: "!" } => true,
            // default
            LiteralExpressionSyntax literal when literal.Token.IsKind(SyntaxKind.DefaultKeyword) => true,
            // default!
            PostfixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax literal, OperatorToken.ValueText: "!" } 
                when literal.Token.IsKind(SyntaxKind.DefaultKeyword) => true,
            _ => false
        };
    }
}