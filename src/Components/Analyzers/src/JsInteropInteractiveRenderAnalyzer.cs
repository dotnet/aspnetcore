// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JsInteropInteractiveRenderAnalyzer : DiagnosticAnalyzer
{
    private const int MaxNestedMethodDepth = 2;
    private static readonly string[] JSInteropParts = new[] { "JSInterop", "Microsoft", };
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(static context =>
        {
            var availableTypes = new Dictionary<string, INamedTypeSymbol?>()
            {
                {
                    ComponentsApi.ComponentBase.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.ComponentBase.MetadataName)
                },
                {
                    ComponentsApi.JSInteropRuntime.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.JSInteropRuntime.MetadataName)
                },
                {
                    ComponentsApi.JSObjectReference.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.JSObjectReference.MetadataName)
                },
                {
                    ComponentsApi.RendererInfo.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.RendererInfo.MetadataName)
                },
                {
                    ComponentsApi.RenderTreeBuilder.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.RenderTreeBuilder.MetadataName)
                },
                {
                    ComponentsApi.EventCallbackFactory.MetadataName,
                    context.Compilation.GetTypeByMetadataName(ComponentsApi.EventCallbackFactory.FullTypeName)
                }
            };

            if (availableTypes[ComponentsApi.ComponentBase.MetadataName] is null
                || availableTypes[ComponentsApi.JSInteropRuntime.MetadataName] is null
                || availableTypes[ComponentsApi.JSObjectReference.MetadataName] is null
                )
            {
                return;
            }

            context.RegisterOperationBlockAction(context =>
            {
                if (context.OwningSymbol is IMethodSymbol owningMethod)
                {
                    if (IsImplementationOfNotSafeMethod(owningMethod, availableTypes))
                    {
                        foreach (var childBlock in context.OperationBlocks)
                        {
                            // Should be one but could be more than one if there are multiple partial class definitions.
                            AnalyzeOperationsTree(childBlock, new JSInteropAnalyzerState(context, availableTypes));
                        }
                    }
                    else if (ComponentFacts.IsBuildRenderTree(owningMethod, availableTypes[ComponentsApi.ComponentBase.MetadataName]))
                    {
                        
                        foreach (var childBlock in context.OperationBlocks)
                        {
                            // Should be one but could be more than one if there are multiple partial class definitions.
                            AnalyzeRendererForHandlers(childBlock, new JSInteropAnalyzerState(context, availableTypes));
                        }
                    }
                }
            });
        });
    }

    private static bool IsImplementationOfNotSafeMethod(IMethodSymbol methodSymbol, Dictionary<string, INamedTypeSymbol?> availableTypes)
    {
        if ((methodSymbol.Name == "OnInitialized" || methodSymbol.Name == "OnInitializedAsync"
            || methodSymbol.Name == "OnParametersSet" || methodSymbol.Name == "OnParametersSetAsync")
            && methodSymbol.IsOverride)
        {
            return ComponentFacts.IsComponentBase(methodSymbol.ContainingType, availableTypes[ComponentsApi.ComponentBase.MetadataName]!);
        }
        return false;
    }

    private static void AnalyzeOperationsTree(IOperation operation, JSInteropAnalyzerState state)
    {
        if (operation is IVariableDeclarationOperation variableDeclaration)
        {
            // Check if `RendererInfo.IsInteractive` is used in a boolean expression before if statement checking it.
            // Also check for JSInterop calls in a ternary operator.
            foreach (var declarator in variableDeclaration.Declarators)
            {
                AnalyzeDeclaration(declarator, state);
            }
        }
        else if (operation is IAssignmentOperation assignment)
        {
            // Check if the assignment is assigning the value of `RendererInfo.IsInteractive` to a local variable.
            AnalyzeAssignment(assignment, state);
        }
        else if (operation is IConditionalOperation condition)
        {
            // Check if the condition is checking `RendererInfo.IsInteractive` or a local variable that has been assigned the value of `RendererInfo.IsInteractive`.
            var isInteractiveChecked = AnalyzeCondition(condition, state);
            state.UpdateIsInteractiveChecked(isInteractiveChecked);
        }
        else if (operation is IInvocationOperation invocation)
        {
            // Analyze if we have JSInterop calls.
            AnalyzeInvocation(invocation, state);
        }
        else
        {
            // Expression statements, blocks, switch, cases etc. that have children.
            // During source-build, we use newer version of Microsoft.CodeAnalysis which obsoletes Children and provide an alternative.
            // However, for normal builds, we use MS.CA 3.3.1 which doesn't obsolete Children and doesn't have an alternative.
            // We suppress the warning for source-builds for now.
#pragma warning disable CS0618
            foreach (var childOperation in operation.Children)
#pragma warning restore CS0618
            {
                if (childOperation is IBlockOperation)
                {
                    // For nested blocks, we don't want to mix variables between same level blocks. Each will have their own scoped list of variables.
                    // Also check for `RendererInfo.IsInteractive` becomes independent in each nested block. Only inherited from parent block.
                    AnalyzeOperationsTree(childOperation, state.Clone());
                }
                else
                {
                    AnalyzeOperationsTree(childOperation, state);
                }
            }
        }
    }

    private static void AnalyzeDeclaration(IVariableDeclaratorOperation declarator, JSInteropAnalyzerState state)
    {
        if (declarator.Initializer?.Value is IOperation initValue
            && OperationChecksIsInteractive(initValue, state, false))
        {
            state.SymbolChecks.Add(declarator.Symbol);
        }
        else if (declarator.Initializer?.Value is IConditionalOperation conditionalOperation)
        {
            // Check for JSInterop usage in ternary operator assignment to a local variable.
            AnalyzeCondition(conditionalOperation, state.Clone());
        }
    }

    private static void AnalyzeAssignment(IAssignmentOperation assignment, JSInteropAnalyzerState state)
    {
        if (assignment.Value is IOperation operation
            && assignment.Target is ILocalReferenceOperation localReference
            && OperationChecksIsInteractive(operation, state, false))
        {
            state.SymbolChecks.Add(localReference.Local);
        }
    }

    private static bool AnalyzeCondition(IConditionalOperation condition, JSInteropAnalyzerState state)
    {
        if (condition.Condition.Type?.SpecialType == SpecialType.System_Boolean)
        {
            // Since we are not certain that the condition checks `RendererInfo.IsInteractive` at all, record both negated and non negated possibilities for the condition.
            var isInteractiveWhenTrue = OperationChecksIsInteractive(condition.Condition, state, false);
            var isInteractiveWhenFalse = OperationChecksIsInteractive(condition.Condition, state, true);
            if (condition.WhenTrue is not null)
            {
                var clonedState = state.Clone();
                clonedState.IsInteractiveChecked = isInteractiveWhenTrue || state.IsInteractiveChecked;
                AnalyzeOperationsTree(condition.WhenTrue, clonedState);
            }
            if (condition.WhenFalse is not null)
            {
                var clonedState = state.Clone();
                clonedState.IsInteractiveChecked = isInteractiveWhenFalse || state.IsInteractiveChecked;
                AnalyzeOperationsTree(condition.WhenFalse, clonedState);
            }

            if (isInteractiveWhenTrue && condition.WhenFalse is not null)
            {
                // If the condition check resolved to just `RendererInfo.IsInteractive`,  `WhenFalse` case should return to avoid JSInterop usage.
                return ConditionBlockReturnsOnIsInteractive(condition.WhenFalse, state);
            }
            else if (isInteractiveWhenFalse && condition.WhenTrue is not null)
            {
                // If the condition check resolved to just `!RendererInfo.IsInteractive`, `WhenTrue` case should return to avoid JSInterop usage.
                return ConditionBlockReturnsOnIsInteractive(condition.WhenTrue, state);
            }

            if (!isInteractiveWhenTrue && !isInteractiveWhenFalse)
            {
                // Analyze the condition for JSInterop usage. Ignore use of `RendererInfo.IsInteractive` in the condition since it is not a direct check for it.
                AnalyzeOperationsTree(condition.Condition, state);
            }
        }
        return false;
    }

    /// <summary>
    /// Analyze condition operation for checks of `RendererInfo.IsInteractive` or local variables that have been assigned the value of condition using `RendererInfo.IsInteractive`.
    /// </summary>
    private static bool OperationChecksIsInteractive(IOperation operation, JSInteropAnalyzerState state, bool negated)
    {
        if (operation is IPropertyReferenceOperation propertyReference
            && IsPropertyRenderInfoInteractive(propertyReference.Property, state.AvailableTypes))
        {
            return !negated;
        }
        else if (operation is ILocalReferenceOperation localReference
            && state.SymbolChecks.Contains(localReference.Local))
        {
            return !negated;
        }
        else if (operation is IUnaryOperation unaryOperation
            && unaryOperation.OperatorKind == UnaryOperatorKind.Not)
        {
            return OperationChecksIsInteractive(unaryOperation.Operand, state, !negated);
        }
        else if (operation is IBinaryOperation binaryOperation)
        {
            bool childConditionChecksInteractive = false;
#pragma warning disable CS0618
            foreach (var childCondition in binaryOperation.Children)
#pragma warning restore CS0618
            {
                childConditionChecksInteractive = childConditionChecksInteractive || OperationChecksIsInteractive(childCondition, state, negated);
            }
            return childConditionChecksInteractive;
        }
        return false;
    }

    /// <summary>
    /// Check if the block of code returns when `RendererInfo.IsInteractive` is checked. If it does, then we mark that the required check has been made for all subsequent code paths.
    /// </summary>
    private static bool ConditionBlockReturnsOnIsInteractive(IOperation operation, JSInteropAnalyzerState state)
    {
        if (operation is IReturnOperation)
        {
            return true;
        }
#pragma warning disable CS0618
        foreach (var childOperation in operation.Children)
#pragma warning restore CS0618
        {
            if (ConditionBlockReturnsOnIsInteractive(childOperation, state))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPropertyRenderInfoInteractive(IPropertySymbol propertyReference, Dictionary<string, INamedTypeSymbol?> availableTypes)
    {
        return propertyReference.Name == "IsInteractive" &&
            SymbolEqualityComparer.Default.Equals(propertyReference.ContainingType, availableTypes[ComponentsApi.RendererInfo.MetadataName]);
    }

    private static bool IsInJSInteropNamespace(INamespaceSymbol symbol)
    {
        var @namespace = symbol;
        for (var i = 0; i < JSInteropParts.Length; i++)
        {
            if (@namespace == null || !string.Equals(JSInteropParts[i], @namespace.Name, StringComparison.Ordinal))
            {
                return false;
            }

            @namespace = @namespace.ContainingNamespace;
        }

        return @namespace.IsGlobalNamespace;
    }

    private static bool IsJSInteropInvocation(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.ContainingNamespace is null)
        {
            return false;
        }

        var containingNamespace = invocation.TargetMethod.ContainingNamespace;
        while (containingNamespace is not null)
        {
            if (IsInJSInteropNamespace(containingNamespace))
            {
                return true;
            }
            containingNamespace = containingNamespace.ContainingNamespace;
        }
        return false;
    }

    private static void AnalyzeInvocation(IInvocationOperation invocation, JSInteropAnalyzerState state)
    {
        if (!state.IsInteractiveChecked && IsJSInteropInvocation(invocation))
        {
            state.BlockContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck,
                    invocation.Syntax.GetLocation(),
                    invocation.TargetMethod.Name));
        }
        else if (invocation.Arguments.Length > 0)
        {
            // Check arguments for JSInterop invocations.
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Value is IInvocationOperation nestedInvocation)
                {
                    AnalyzeOperationsTree(nestedInvocation, state);
                }
            }
        }
        else if (state.CurrentDepth < MaxNestedMethodDepth
            && state.BlockContext.OwningSymbol is IMethodSymbol methodSymbol
            && ComponentFacts.IsComponentBase(methodSymbol.ContainingType, state.AvailableTypes[ComponentsApi.ComponentBase.MetadataName]))
        {
            AnalyzeMethodReference(invocation.TargetMethod, state);
        }
    }

    private static void AnalyzeMethodReference(IMethodSymbol methodSymbol, JSInteropAnalyzerState state)
    {
        foreach (var syntaxRef in methodSymbol.DeclaringSyntaxReferences)
        {
            var syntaxNode = syntaxRef.GetSyntax();
            if (syntaxNode is MethodDeclarationSyntax methodDecl)
            {
                var methodOperation = state.BlockContext.Compilation.GetSemanticModel(methodDecl.SyntaxTree).GetOperation(methodDecl);
                if (methodOperation is null)
                {
                    continue;
                }
                var clonedState = state.Clone();
                clonedState.CurrentDepth++;
                AnalyzeOperationsTree(methodOperation, clonedState);
            }
        }
    }

    private static void AnalyzeRendererForHandlers(IOperation operation, JSInteropAnalyzerState state)
    {
        if (operation is IInvocationOperation invocation)
        {
            // Analyze if we have event handler, so we can check it for JSInterop calls.
            AnalyzeInvocationForHandler(invocation, state);
        }
        else
        {
            // Expression statements, blocks, switch, cases etc. that have children.
#pragma warning disable CS0618
            foreach (var childOperation in operation.Children)
#pragma warning restore CS0618
            {
                AnalyzeRendererForHandlers(childOperation, state);
            }
        }
    }

    private static void AnalyzeInvocationForHandler(IInvocationOperation invocation, JSInteropAnalyzerState state)
    {
        if (invocation.Instance?.Type is not null
            && invocation.Arguments.Length >= 3)
        {
            var targetMethod = invocation.TargetMethod;
            if (targetMethod.Name != "AddAttribute" && targetMethod.Name != "AddComponentParameter")
            {
                return;
            }

            // Get the third argument, which is the value of the attribute/parameter. If it's a delegate, we need to analyze it for JSInterop calls.
            IOperation? suspectOperation = null;
            var valueArgument = invocation.Arguments[2].Value;
            if (valueArgument is IInvocationOperation invocationOperation)
            {
                if (SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ContainingType, state.AvailableTypes[ComponentsApi.EventCallbackFactory.MetadataName])
                    && invocationOperation.TargetMethod.Name != "CreateBinder"
                    && invocationOperation.Arguments.Length >= 2
                    && invocationOperation.Arguments[1].Value is IDelegateCreationOperation delegateCreation)
                {
                    suspectOperation = delegateCreation.Target;
                }
            }
            else if (valueArgument is IDelegateCreationOperation delegateCreation
                && (delegateCreation.Target is IAnonymousFunctionOperation || delegateCreation.Target is IMethodReferenceOperation))
            {
                suspectOperation = delegateCreation.Target;
            }
            else if (valueArgument is IConversionOperation conversionOperation
                && conversionOperation.Operand.Type is not null && conversionOperation.Operand.Type.ContainingNamespace is not null
                && conversionOperation.Operand.Type.ContainingNamespace.ToString().StartsWith(ComponentsApi.AssemblyName, StringComparison.Ordinal))
            {
                // If the value is a conversion operation, search if a delegate is created like RenderFragment or an EventCallback.
                // Multiple delegates at once shouldn't be possible.
                var delegateResult = FindFirstDelegateChild(conversionOperation.Operand);
                if (delegateResult is not null
                    && (delegateResult.Target is IAnonymousFunctionOperation || delegateResult.Target is IMethodReferenceOperation))
                {
                    suspectOperation = delegateResult.Target;
                }
            }

            if (suspectOperation is IAnonymousFunctionOperation anonymousFunction)
            {
                AnalyzeOperationsTree(anonymousFunction.Body, state);
            }
            else if (suspectOperation is IMethodReferenceOperation methodReference)
            {
                AnalyzeMethodReference(methodReference.Method, state);
            }
        }
    }

    /// <summary>
    /// Probe an operation for delegate creation. If found, return the first occurrence. Otherwise search only inside Conversion or Invocation operations.
    /// Should be more efficient than calling Descendants() and filtering for IDelegateCreationOperation, since we don't need to search inside all operations.
    /// </summary>
    private static IDelegateCreationOperation? FindFirstDelegateChild(IOperation currentOperation)
    {
        if (currentOperation is IDelegateCreationOperation delegateCreation)
        {
            return delegateCreation;
        }

        if (currentOperation is IArgumentOperation
            || currentOperation is IConversionOperation
            || currentOperation is IInvocationOperation)
        {
#pragma warning disable CS0618
            foreach (var child in currentOperation.Children)
#pragma warning restore CS0618
            {
                var result = FindFirstDelegateChild(child);
                if (result is not null)
                {
                    return result;
                }
            }
        }
        return null;
    }

    private class JSInteropAnalyzerState
    {
        public int CurrentDepth { get; set; }
        public bool IsInteractiveChecked { get; set; }
        public Dictionary<string, INamedTypeSymbol?> AvailableTypes { get; }
        public OperationBlockAnalysisContext BlockContext { get; }

        public HashSet<ILocalSymbol> SymbolChecks { get; private set; } = new HashSet<ILocalSymbol>();

        public JSInteropAnalyzerState(OperationBlockAnalysisContext blockContext, Dictionary<string, INamedTypeSymbol?> availableTypes)
        {
            BlockContext = blockContext;
            AvailableTypes = availableTypes;
        }

        public JSInteropAnalyzerState Clone()
        {
            return new JSInteropAnalyzerState(this.BlockContext, this.AvailableTypes)
            {
                CurrentDepth = this.CurrentDepth,
                IsInteractiveChecked = this.IsInteractiveChecked,
                SymbolChecks = new (this.SymbolChecks)
            };
        }

        public void UpdateIsInteractiveChecked(bool value)
        {
            IsInteractiveChecked = IsInteractiveChecked || value;
        }
    }
}
