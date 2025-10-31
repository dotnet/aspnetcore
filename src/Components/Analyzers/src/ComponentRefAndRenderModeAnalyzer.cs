// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Only analyze methods that appear to be Blazor component BuildRenderTree methods
        if (!IsComponentBuildRenderTreeMethod(methodDeclaration))
        {
            return;
        }

        // Find all invocation expressions in the method
        var invocations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

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

    private static bool IsComponentBuildRenderTreeMethod(MethodDeclarationSyntax method)
    {
        // Check if this looks like a BuildRenderTree method or similar component method
        // These methods typically contain calls to RenderTreeBuilder methods
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => 
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var memberName = memberAccess.Name.Identifier.ValueText;
                    return memberName is "OpenComponent" or "AddComponentParameter" or "AddComponentRenderMode" or "AddComponentReferenceCapture";
                }
                return false;
            });
    }

    private static System.Collections.Generic.List<(InvocationExpressionSyntax OpenComponent, System.Collections.Generic.List<InvocationExpressionSyntax> ComponentCalls)> AnalyzeComponentBlocks(
        System.Collections.Generic.List<InvocationExpressionSyntax> invocations, 
        SemanticModel semanticModel)
    {
        var componentBlocks = new System.Collections.Generic.List<(InvocationExpressionSyntax, System.Collections.Generic.List<InvocationExpressionSyntax>)>();
        InvocationExpressionSyntax? currentOpenComponent = null;
        var currentComponentCalls = new System.Collections.Generic.List<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (IsOpenComponent(invocation, semanticModel))
            {
                // If we have a previous component block, save it
                if (currentOpenComponent is not null)
                {
                    componentBlocks.Add((currentOpenComponent, currentComponentCalls));
                }

                // Start a new component block
                currentOpenComponent = invocation;
                currentComponentCalls = new System.Collections.Generic.List<InvocationExpressionSyntax>();
            }
            else if (IsCloseComponent(invocation, semanticModel))
            {
                // End the current component block
                if (currentOpenComponent is not null)
                {
                    componentBlocks.Add((currentOpenComponent, currentComponentCalls));
                    currentOpenComponent = null;
                    currentComponentCalls = new System.Collections.Generic.List<InvocationExpressionSyntax>();
                }
            }
            else if (currentOpenComponent is not null && IsComponentRelatedCall(invocation, semanticModel))
            {
                // Add to the current component block
                currentComponentCalls.Add(invocation);
            }
        }

        // Handle case where method ends without CloseComponent
        if (currentOpenComponent is not null)
        {
            componentBlocks.Add((currentOpenComponent, currentComponentCalls));
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