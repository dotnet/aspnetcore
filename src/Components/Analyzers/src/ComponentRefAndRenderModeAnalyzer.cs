// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentRefAndRenderModeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement);

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

            context.RegisterSymbolStartAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;

                // Only analyze types that are components
                if (!ComponentFacts.IsComponent(symbols, context.Compilation, type))
                {
                    return;
                }

                context.RegisterSyntaxNodeAction(context =>
                {
                    var methodDeclaration = (MethodDeclarationSyntax)context.Node;

                    // For ComponentBase types, we specifically look for BuildRenderTree method
                    if (symbols.ComponentBaseType != null && 
                        ComponentFacts.IsComponentBase(symbols, type) &&
                        methodDeclaration.Identifier.ValueText != "BuildRenderTree")
                    {
                        return;
                    }

                    AnalyzeMethod(context, methodDeclaration);
                }, SyntaxKind.MethodDeclaration);
            }, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        // Find all invocation expressions in the method
        var invocations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        // Quick check: if there are no invocations, nothing to analyze
        if (invocations.Count == 0)
        {
            return;
        }

        // Group invocations by the component they're operating on by looking at OpenComponent/CloseComponent pairs
        var componentBlocks = AnalyzeComponentBlocks(invocations, context.SemanticModel);

        foreach (var (openComponentCall, componentCalls) in componentBlocks)
        {
            var hasReferenceCapture = componentCalls.Any(call => IsAddComponentReferenceCapture(call, context.SemanticModel));
            var hasRenderMode = componentCalls.Any(call => IsAddComponentRenderMode(call, context.SemanticModel));

            if (hasReferenceCapture && hasRenderMode)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement,
                    openComponentCall.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static List<(InvocationExpressionSyntax OpenComponent, List<InvocationExpressionSyntax> ComponentCalls)> AnalyzeComponentBlocks(
        List<InvocationExpressionSyntax> invocations, 
        SemanticModel semanticModel)
    {
        var componentBlocks = new List<(InvocationExpressionSyntax, List<InvocationExpressionSyntax>)>();
        var componentStack = new Stack<(InvocationExpressionSyntax OpenComponentInvocation, List<InvocationExpressionSyntax> RelatedInvocations)>();

        foreach (var invocation in invocations)
        {
            if (IsOpenComponent(invocation, semanticModel))
            {
                var newComponentBlock = (invocation, new List<InvocationExpressionSyntax>());
                componentStack.Push(newComponentBlock);
            }
            else if (IsCloseComponent(invocation, semanticModel))
            {
                if (componentStack.Count > 0)
                {
                    var completedComponentBlock = componentStack.Pop();
                    componentBlocks.Add(completedComponentBlock);
                }
            }
            else if (IsComponentRelatedCall(invocation, semanticModel))
            {
                if (componentStack.Count > 0)
                {
                    var currentComponentBlock = componentStack.Peek();
                    currentComponentBlock.RelatedInvocations.Add(invocation);
                }
            }
        }

        // Handle case where method ends without CloseComponent
        while (componentStack.Count > 0)
        {
            var remainingComponentBlock = componentStack.Pop();
            componentBlocks.Add(remainingComponentBlock);
        }

        return componentBlocks;
    }

    private static bool IsOpenComponent(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsMethodCall(invocation, semanticModel, "OpenComponent");
    }

    private static bool IsCloseComponent(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsMethodCall(invocation, semanticModel, "CloseComponent");
    }

    private static bool IsAddComponentReferenceCapture(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsMethodCall(invocation, semanticModel, "AddComponentReferenceCapture");
    }

    private static bool IsAddComponentRenderMode(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsMethodCall(invocation, semanticModel, "AddComponentRenderMode");
    }

    private static bool IsComponentRelatedCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsMethodCall(invocation, semanticModel, "AddComponentParameter") ||
               IsMethodCall(invocation, semanticModel, "AddComponentReferenceCapture") ||
               IsMethodCall(invocation, semanticModel, "AddComponentRenderMode");
    }

    private static bool IsMethodCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string methodName)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.ValueText == methodName)
        {
            // Additional validation: check if this is actually called on RenderTreeBuilder
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                return method.ContainingType?.Name == "RenderTreeBuilder" &&
                       method.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering";
            }
        }

        return false;
    }
}