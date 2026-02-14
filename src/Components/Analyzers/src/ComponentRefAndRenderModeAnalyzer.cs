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
    private readonly struct ComponentBlock
    {
        public ComponentBlock(InvocationExpressionSyntax openComponent, List<InvocationExpressionSyntax> componentCalls)
        {
            OpenComponent = openComponent;
            ComponentCalls = componentCalls;
        }

        public InvocationExpressionSyntax OpenComponent { get; }
        public List<InvocationExpressionSyntax> ComponentCalls { get; }
    }

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

                    AnalyzeMethod(symbols, context, methodDeclaration);
                }, SyntaxKind.MethodDeclaration);
            }, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeMethod(ComponentSymbols symbols, SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        // Find all invocation expressions in the method
        var invocations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        // Quick check: if there are no invocations, nothing to analyze
        if (invocations.Count == 0)
        {
            return;
        }

        // Group invocations by the component they're operating on by looking at OpenComponent/CloseComponent pairs
        var componentBlocks = AnalyzeComponentBlocks(symbols, invocations, context.SemanticModel);

        foreach (var componentBlock in componentBlocks)
        {
            var hasReferenceCapture = componentBlock.ComponentCalls.Any(call => IsAddComponentReferenceCapture(symbols, call, context.SemanticModel));
            var hasRenderMode = componentBlock.ComponentCalls.Any(call => IsAddComponentRenderMode(symbols, call, context.SemanticModel));

            if (hasReferenceCapture && hasRenderMode)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement,
                    componentBlock.OpenComponent.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static List<ComponentBlock> AnalyzeComponentBlocks(
        ComponentSymbols symbols,
        List<InvocationExpressionSyntax> invocations, 
        SemanticModel semanticModel)
    {
        var componentBlocks = new List<ComponentBlock>();
        var componentStack = new Stack<ComponentBlock>();

        foreach (var invocation in invocations)
        {
            if (ComponentFacts.IsOpenComponentInvocation(symbols, invocation, semanticModel))
            {
                var newComponentBlock = new ComponentBlock(invocation, new List<InvocationExpressionSyntax>());
                componentStack.Push(newComponentBlock);
            }
            else if (ComponentFacts.IsCloseComponentInvocation(symbols, invocation, semanticModel))
            {
                if (componentStack.Count > 0)
                {
                    var completedComponentBlock = componentStack.Pop();
                    componentBlocks.Add(completedComponentBlock);
                }
            }
            else if (IsComponentRelatedCall(symbols, invocation, semanticModel))
            {
                if (componentStack.Count > 0)
                {
                    var currentComponentBlock = componentStack.Peek();
                    currentComponentBlock.ComponentCalls.Add(invocation);
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

    private static bool IsAddComponentReferenceCapture(ComponentSymbols symbols, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return ComponentFacts.IsRenderTreeBuilderMethodInvocation(invocation, semanticModel, symbols.AddComponentReferenceCaptureMethod);
    }

    private static bool IsAddComponentRenderMode(ComponentSymbols symbols, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return ComponentFacts.IsRenderTreeBuilderMethodInvocation(invocation, semanticModel, symbols.AddComponentRenderModeMethod);
    }

    private static bool IsComponentRelatedCall(ComponentSymbols symbols, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return ComponentFacts.IsRenderTreeBuilderMethodInvocation(invocation, semanticModel, symbols.AddComponentParameterMethod) ||
               ComponentFacts.IsRenderTreeBuilderMethodInvocation(invocation, semanticModel, symbols.AddComponentReferenceCaptureMethod) ||
               ComponentFacts.IsRenderTreeBuilderMethodInvocation(invocation, semanticModel, symbols.AddComponentRenderModeMethod);
    }
}