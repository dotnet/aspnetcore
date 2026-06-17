// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StateHasChangedAnalyzer : DiagnosticAnalyzer
{
    private const string EventCallbackFactoryTypeName = "Microsoft.AspNetCore.Components.EventCallbackFactory";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnnecessaryStateHasChangedCall);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolStartAction(context =>
        {
            if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
            {
                // Types we need are not defined.
                return;
            }

            if (symbols.ComponentBaseType is null)
            {
                // ComponentBase availability guard.
                return;
            }

            var type = (INamedTypeSymbol)context.Symbol;
            if (!ComponentFacts.IsComponentBase(symbols, type))
            {
                // only applies to ComponentBase derived types.
                return;
            }

            var eventCallbackFactoryType = context.Compilation.GetTypeByMetadataName(EventCallbackFactoryTypeName);
            var eventHandlerMethods = new ConcurrentDictionary<IMethodSymbol, byte>(SymbolEqualityComparer.Default);
            var redundantCallLocations = new ConcurrentBag<Location>();

            // collect event handler methods
            context.RegisterOperationAction(operationContext =>
            {
                if (eventCallbackFactoryType is null)
                {
                    return;
                }

                var invocation = (IInvocationOperation)operationContext.Operation;
                if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, eventCallbackFactoryType))
                {
                    return;
                }

                foreach (var argument in invocation.Arguments)
                {
                    var method = TryGetMethodFromOperation(argument.Value);
                    if (method is not null)
                    {
                        eventHandlerMethods.TryAdd(method, 0);
                    }
                }
            }, OperationKind.Invocation);

            // collect unnecessary StateHasChanged calls
            context.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var methodDeclaration = (MethodDeclarationSyntax)syntaxContext.Node;

                if (syntaxContext.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
                {
                    return;
                }

                if (!IsTargetMethod(methodSymbol, eventHandlerMethods))
                {
                    return;
                }

                var body = methodDeclaration.Body;
                if (body is null)
                {
                    return;
                }

                var awaitExpressions = body.DescendantNodes(static node => !IsNestedFunctionLike(node)).OfType<AwaitExpressionSyntax>().OrderBy(n => n.SpanStart).ToList();
                var stateCalls = body.DescendantNodes(static node => !IsNestedFunctionLike(node)).OfType<InvocationExpressionSyntax>()
                    .Where(invocation => IsStateHasChangedCall(syntaxContext.SemanticModel, invocation))
                    .OrderBy(invocation => invocation.SpanStart)
                    .ToList();
                if (stateCalls.Count == 0)
                {
                    // no call, no problems.
                    return;
                }

                var callLocations = new Dictionary<int, Location>();
                if (awaitExpressions.Count == 0)
                {
                    // no await expressions, all calls are potentially redundant
                    foreach (var stateCall in stateCalls)
                    {
                        AddCallLocation(callLocations, stateCall);
                    }
                }
                else
                {
                    var firstAwaitStart = awaitExpressions[0].SpanStart;
                    var lastAwaitStart = awaitExpressions[awaitExpressions.Count - 1].SpanStart;
                    foreach (var stateCall in stateCalls)
                    {
                        if (stateCall.SpanStart < firstAwaitStart || stateCall.SpanStart > lastAwaitStart)
                        {
                            // any calls before the first await or after the last one are redundant, because ComponentBase calls StateHasChanged afterwards.
                            AddCallLocation(callLocations, stateCall);
                        }
                    }
                }

                if (callLocations.Count == 0)
                {
                    return;
                }

                foreach (var location in callLocations.Values)
                {
                    redundantCallLocations.Add(location);
                }
            }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);

            context.RegisterSymbolEndAction(endContext =>
            {
                foreach (var location in redundantCallLocations)
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnnecessaryStateHasChangedCall,
                        location));
                }
            });
        }, SymbolKind.NamedType);
    }

    // Targets of this analyzer are lifecycle methods (OnInitialized, OnParametersSet) and event handlers
    private static bool IsTargetMethod(IMethodSymbol method, ConcurrentDictionary<IMethodSymbol, byte> eventHandlerMethods)
    {
        if (method.MethodKind != MethodKind.Ordinary)
        {
            return false;
        }

        if (IsTargetLifecycleMethod(method.OverriddenMethod ?? method))
        {
            return true;
        }

        return eventHandlerMethods.ContainsKey(method);
    }

    private static bool IsTargetLifecycleMethod(IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Ordinary &&
            method.Parameters.Length == 0 &&
            method.Name is "OnInitialized" or "OnInitializedAsync" or "OnParametersSet" or "OnParametersSetAsync";
    }

    private static bool IsStateHasChangedCall(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
    {
        return semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
            method.MethodKind == MethodKind.Ordinary &&
            method.Parameters.Length == 0 &&
            method.Name == "StateHasChanged";
    }

    private static void AddCallLocation(Dictionary<int, Location> callLocations, InvocationExpressionSyntax stateCall)
    {
        if (!callLocations.ContainsKey(stateCall.SpanStart))
        {
            callLocations[stateCall.SpanStart] = stateCall.GetLocation();
        }
    }

    // Exclude local functions/lambdas because those have their own flow and execution timing.
    private static bool IsNestedFunctionLike(SyntaxNode node)
    {
        return node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;
    }

    private static IMethodSymbol? TryGetMethodFromOperation(IOperation operation)
    {
        switch (operation)
        {
            case IMethodReferenceOperation methodReference:
                return methodReference.Method;
            case IDelegateCreationOperation delegateCreation when delegateCreation.Target is not null:
                return TryGetMethodFromOperation(delegateCreation.Target);
            case IConversionOperation conversion:
                return TryGetMethodFromOperation(conversion.Operand);
            default:
                return null;
        }
    }
}
