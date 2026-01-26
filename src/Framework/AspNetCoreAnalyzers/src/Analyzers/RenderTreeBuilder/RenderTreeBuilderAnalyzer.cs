// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RenderTreeBuilderAnalyzer : DiagnosticAnalyzer
{
    private const int SequenceParameterOrdinal = 0;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
        DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;

                if (!IsRenderTreeBuilderMethodWithSequenceParameter(wellKnownTypes, invocation.TargetMethod))
                {
                    return;
                }

                foreach (var argument in invocation.Arguments)
                {
                    if (argument.Parameter?.Ordinal == SequenceParameterOrdinal)
                    {
                        if (!argument.Value.Syntax.IsKind(SyntaxKind.NumericLiteralExpression))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
                                argument.Syntax.GetLocation(),
                                argument.Syntax.ToString()));
                        }

                        break;
                    }
                }

            }, OperationKind.Invocation);

            // Get ComponentBase type for scoping local function analysis
            var componentBaseType = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");

            // Register syntax node action to detect local functions within BuildRenderTree methods
            context.RegisterSyntaxNodeAction(context =>
            {
                var methodDeclaration = (MethodDeclarationSyntax)context.Node;

                // Only analyze BuildRenderTree methods
                if (methodDeclaration.Identifier.ValueText != "BuildRenderTree")
                {
                    return;
                }

                // Check if the containing type extends ComponentBase
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                if (methodSymbol?.ContainingType == null || componentBaseType == null)
                {
                    return;
                }

                if (!InheritsFromComponentBase(methodSymbol.ContainingType, componentBaseType))
                {
                    return;
                }

                // Now check for local functions within this BuildRenderTree method
                var localFunctions = methodDeclaration.DescendantNodes().OfType<LocalFunctionStatementSyntax>();
                foreach (var localFunction in localFunctions)
                {
                    if (ContainsRenderTreeBuilderCalls(wellKnownTypes, localFunction, context.SemanticModel))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup,
                            localFunction.Identifier.GetLocation(),
                            localFunction.Identifier.ValueText));
                    }
                }
            }, SyntaxKind.MethodDeclaration);
        });
    }

    private static bool IsRenderTreeBuilderMethodWithSequenceParameter(WellKnownTypes wellKnownTypes, IMethodSymbol targetMethod)
        => SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Components_Rendering_RenderTreeBuilder), targetMethod.ContainingType)
        && targetMethod.Parameters.Length > SequenceParameterOrdinal
        && targetMethod.Parameters[SequenceParameterOrdinal].Name == "sequence";

    private static bool InheritsFromComponentBase(INamedTypeSymbol type, INamedTypeSymbol componentBaseType)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, componentBaseType))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private static bool ContainsRenderTreeBuilderCalls(WellKnownTypes wellKnownTypes, LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel)
    {
        var renderTreeBuilderType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Components_Rendering_RenderTreeBuilder);
        if (renderTreeBuilderType is null)
        {
            return false;
        }

        // Static local functions cannot capture from enclosing scope, so they're safe
        if (localFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return false;
        }

        // Walk through all invocation expressions in the local function
        var invocations = localFunction.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        foreach (var invocation in invocations)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol method &&
                SymbolEqualityComparer.Default.Equals(renderTreeBuilderType, method.ContainingType))
            {
                // Check if this is a call on a captured variable (not a parameter)
                if (IsCallOnCapturedRenderTreeBuilder(invocation, localFunction, semanticModel, renderTreeBuilderType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsCallOnCapturedRenderTreeBuilder(InvocationExpressionSyntax invocation, LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, INamedTypeSymbol _)
    {
        // Get the expression that the method is being called on
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess is null)
        {
            return false;
        }

        var targetSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
        
        // If it's a parameter of the local function, it's not captured
        if (targetSymbol is IParameterSymbol parameter)
        {
            // Check if this parameter belongs to our local function
            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction);
            return localFunctionSymbol is not null && !localFunctionSymbol.Parameters.Contains(parameter, SymbolEqualityComparer.Default);
        }

        // If it's a local variable or field, it could be captured
        return targetSymbol is IFieldSymbol or ILocalSymbol;
    }
}
