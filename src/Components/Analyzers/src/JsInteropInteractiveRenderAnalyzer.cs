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

    // Types in Microsoft.JSInterop whose methods call into JavaScript. Other JSInterop APIs, such as DotNetObjectReference, do not.
    private static readonly HashSet<string> JSInteropCallTypeNames = new(StringComparer.Ordinal)
    {
        "IJSRuntime",
        "IJSInProcessRuntime",
        "JSRuntime",
        "JSInProcessRuntime",
        "JSRuntimeExtensions",
        "JSInProcessRuntimeExtensions",
        "IJSObjectReference",
        "IJSInProcessObjectReference",
        "JSObjectReference",
        "JSInProcessObjectReference",
        "JSObjectReferenceExtensions",
        "JSInProcessObjectReferenceExtensions",
        "IJSStreamReference",
        "JSStreamReference",
    };
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
                if (context.OwningSymbol is IMethodSymbol owningMethod
                    && (IsImplementationOfNotSafeMethod(owningMethod, availableTypes)
                        || ComponentFacts.IsBuildRenderTree(owningMethod, availableTypes[ComponentsApi.ComponentBase.MetadataName])))
                {
                    foreach (var childBlock in context.OperationBlocks)
                    {
                        // Should be one but could be more than one if there are multiple partial class definitions.
                        AnalyzeOperationsTree(childBlock, new JSInteropAnalyzerState(context, availableTypes));
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
        else if (operation is ITryOperation tryOperation)
        {
            AnalyzeTry(tryOperation, state);
        }
        else if (operation is IInvocationOperation invocation)
        {
            // Analyze if we have JSInterop calls.
            AnalyzeInvocation(invocation, state);
        }
        else if (operation is ILocalFunctionOperation)
        {
            return;
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
        else if (declarator.Initializer?.Value is IOperation value)
        {
            AnalyzeAssignedValue(declarator.Symbol, value, state);
        }
    }

    private static void AnalyzeAssignment(IAssignmentOperation assignment, JSInteropAnalyzerState state)
    {
        if (assignment.Target is ILocalReferenceOperation localReference
            && OperationChecksIsInteractive(assignment.Value, state, false))
        {
            state.SymbolChecks.Add(localReference.Local);
            return;
        }

        var reassignedLocal = (assignment.Target as ILocalReferenceOperation)?.Local;
        if (reassignedLocal is not null)
        {
            state.SymbolChecks.Remove(reassignedLocal);
        }

        AnalyzeAssignedValue(reassignedLocal, assignment.Value, state);
    }

    private static void AnalyzeAssignedValue(ILocalSymbol? assignedLocal, IOperation value, JSInteropAnalyzerState state)
    {
        if (assignedLocal is not null)
        {
            state.StoredDelegates.Remove(assignedLocal);
        }

        if (value is IConditionalOperation conditionalOperation)
        {
            // Check for JSInterop usage in ternary operator assignment.
            var isInteractiveChecked = AnalyzeCondition(conditionalOperation, state.Clone());
            state.UpdateIsInteractiveChecked(isInteractiveChecked);
        }
        else if (value is IDelegateCreationOperation delegateCreation)
        {
            // Stored delegates are analyzed where they are invoked, since that is where an IsInteractive check applies.
            if (assignedLocal is not null
                && (delegateCreation.Target is IAnonymousFunctionOperation || delegateCreation.Target is IMethodReferenceOperation))
            {
                state.StoredDelegates[assignedLocal] = delegateCreation.Target;
            }
        }
        else
        {
            AnalyzeOperationsTree(value, state);
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
                return ConditionBlockReturnsOnIsInteractive(condition.WhenFalse);
            }
            else if (isInteractiveWhenFalse && condition.WhenTrue is not null)
            {
                // If the condition check resolved to just `!RendererInfo.IsInteractive`, `WhenTrue` case should return to avoid JSInterop usage.
                return ConditionBlockReturnsOnIsInteractive(condition.WhenTrue);
            }

            if (!isInteractiveWhenTrue && !isInteractiveWhenFalse)
            {
                // Analyze the condition for JSInterop usage. Ignore use of `RendererInfo.IsInteractive` in the condition since it is not a direct check for it.
                AnalyzeOperationsTree(condition.Condition, state);
            }
        }
        return false;
    }

    private static void AnalyzeTry(ITryOperation tryOperation, JSInteropAnalyzerState state)
    {
        var bodyState = state.Clone();
        AnalyzeOperationsTree(tryOperation.Body, bodyState);
        var isInteractiveChecked = IsCheckedOrNeverCompletes(tryOperation.Body, bodyState);

        // An exception can enter a catch handler before any statement of the try body completes, so handlers start from the state before the try.
        foreach (var catchClause in tryOperation.Catches)
        {
            var catchState = state.Clone();
            if (catchClause.Filter is not null)
            {
                AnalyzeOperationsTree(catchClause.Filter, catchState);
            }

            AnalyzeOperationsTree(catchClause.Handler, catchState);
            isInteractiveChecked = isInteractiveChecked && IsCheckedOrNeverCompletes(catchClause.Handler, catchState);
        }

        if (tryOperation.Finally is not null)
        {
            AnalyzeOperationsTree(tryOperation.Finally, state.Clone());
        }

        state.UpdateIsInteractiveChecked(isInteractiveChecked);
    }

    private static bool IsCheckedOrNeverCompletes(IOperation path, JSInteropAnalyzerState pathState)
    {
        return pathState.IsInteractiveChecked || ConditionBlockReturnsOnIsInteractive(path);
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
        else if (operation is IBinaryOperation equalityOperation
            && TryGetComparisonToBooleanConstant(equalityOperation, out var comparedOperation, out var comparisonNegates))
        {
            return OperationChecksIsInteractive(comparedOperation, state, negated != comparisonNegates);
        }
        // This analyzer builds against Microsoft.CodeAnalysis 3.3.1 to support VS 2019, which predates C# 9 patterns.
        // `is not true` and `is not false` therefore cannot be matched and are not treated as IsInteractive checks.
        else if (operation is IIsPatternOperation isPatternOperation
            && isPatternOperation.Pattern is IConstantPatternOperation constantPattern
            && constantPattern.Value.ConstantValue.HasValue
            && constantPattern.Value.ConstantValue.Value is bool patternConstant)
        {
            return OperationChecksIsInteractive(isPatternOperation.Value, state, negated != !patternConstant);
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
    /// Match `operand == true`, `operand != false` and their negating forms, with the constant on either side.
    /// </summary>
    private static bool TryGetComparisonToBooleanConstant(IBinaryOperation equalityOperation, out IOperation comparedOperation, out bool comparisonNegates)
    {
        comparedOperation = equalityOperation;
        comparisonNegates = false;
        if (equalityOperation.OperatorKind != BinaryOperatorKind.Equals && equalityOperation.OperatorKind != BinaryOperatorKind.NotEquals)
        {
            return false;
        }

        bool constant;
        if (equalityOperation.RightOperand.ConstantValue is { HasValue: true, Value: bool rightConstant })
        {
            comparedOperation = equalityOperation.LeftOperand;
            constant = rightConstant;
        }
        else if (equalityOperation.LeftOperand.ConstantValue is { HasValue: true, Value: bool leftConstant })
        {
            comparedOperation = equalityOperation.RightOperand;
            constant = leftConstant;
        }
        else
        {
            return false;
        }

        comparisonNegates = constant == (equalityOperation.OperatorKind == BinaryOperatorKind.NotEquals);
        return true;
    }

    /// <summary>
    /// Check if the block of code returns when `RendererInfo.IsInteractive` is checked. If it does, then we mark that the required check has been made for all subsequent code paths.
    /// </summary>
    private static bool ConditionBlockReturnsOnIsInteractive(IOperation operation)
    {
        if (operation.Syntax is StatementSyntax statementSyntax
            && operation.SemanticModel is SemanticModel semanticModel)
        {
            var controlFlow = semanticModel.AnalyzeControlFlow(statementSyntax);
            if (controlFlow.Succeeded)
            {
                return !controlFlow.EndPointIsReachable;
            }
        }

        return OperationAlwaysTerminates(operation);
    }

    private static bool OperationAlwaysTerminates(IOperation operation)
    {
        // A throw expression in a conditional expression is converted to the type of the other branch.
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is IReturnOperation || operation is IThrowOperation)
        {
            return true;
        }

        if (operation is IBlockOperation blockOperation)
        {
            foreach (var childOperation in blockOperation.Operations)
            {
                if (OperationAlwaysTerminates(childOperation))
                {
                    return true;
                }
            }

            return false;
        }

        if (operation is IConditionalOperation conditionalOperation
            && conditionalOperation.WhenFalse is not null)
        {
            return OperationAlwaysTerminates(conditionalOperation.WhenTrue)
                && OperationAlwaysTerminates(conditionalOperation.WhenFalse);
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
        var containingType = invocation.TargetMethod.ContainingType;
        return JSInteropCallTypeNames.Contains(containingType.Name)
            && IsInJSInteropNamespaceOrNested(containingType.ContainingNamespace);
    }

    private static bool IsInJSInteropNamespaceOrNested(INamespaceSymbol? containingNamespace)
    {
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
        if (TryAnalyzeRenderTreeCallbackRegistration(invocation, state))
        {
            // RenderTreeBuilder AddAttribute/AddComponentParameter call that registers a callback was already analyzed in the context where it is registered.
            // Interactive-only DOM event attributes are skipped
            return;
        }

        // Any other invocation, including RenderTreeBuilder calls without a callback, is analyzed as part of the normal flow.
        if (!state.IsInteractiveChecked && IsJSInteropInvocation(invocation))
        {
            state.BlockContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck,
                    invocation.Syntax.GetLocation(),
                    invocation.TargetMethod.Name));
        }

        if (invocation.Instance is not null)
        {
            AnalyzeOperationsTree(invocation.Instance, state);
        }

        foreach (var argument in invocation.Arguments)
        {
            var parameterRefKind = argument.Parameter?.RefKind;
            if ((parameterRefKind == RefKind.Ref || parameterRefKind == RefKind.Out)
                && argument.Value is ILocalReferenceOperation localReference)
            {
                state.SymbolChecks.Remove(localReference.Local);
            }
            else
            {
                AnalyzeOperationsTree(argument.Value, state);
            }
        }

        if (state.CurrentDepth < MaxNestedMethodDepth
            && FindInvokedStoredDelegate(invocation, state) is IOperation storedDelegate)
        {
            AnalyzeStoredDelegate(storedDelegate, state);
        }

        if (state.CurrentDepth < MaxNestedMethodDepth
            && state.BlockContext.OwningSymbol is IMethodSymbol methodSymbol
            && ComponentFacts.IsComponentBase(methodSymbol.ContainingType, state.AvailableTypes[ComponentsApi.ComponentBase.MetadataName]))
        {
            AnalyzeMethodReference(invocation.TargetMethod, state);
        }
    }

    private static IOperation? FindInvokedStoredDelegate(IInvocationOperation invocation, JSInteropAnalyzerState state)
    {
        if (invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke
            && GetInvokedDelegate(invocation) is ILocalReferenceOperation localReference
            && state.StoredDelegates.TryGetValue(localReference.Local, out var storedDelegate))
        {
            return storedDelegate;
        }

        return null;
    }

    private static IOperation? GetInvokedDelegate(IInvocationOperation invocation)
    {
        if (invocation.Instance is not IConditionalAccessInstanceOperation)
        {
            return invocation.Instance;
        }

        for (var parent = invocation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IConditionalAccessOperation conditionalAccess)
            {
                return conditionalAccess.Operation;
            }
        }

        return null;
    }

    private static void AnalyzeStoredDelegate(IOperation storedDelegate, JSInteropAnalyzerState state)
    {
        if (storedDelegate is IAnonymousFunctionOperation anonymousFunction)
        {
            var clonedState = state.Clone();
            clonedState.CurrentDepth++;
            AnalyzeOperationsTree(anonymousFunction.Body, clonedState);
        }
        else if (storedDelegate is IMethodReferenceOperation methodReference)
        {
            AnalyzeMethodReference(methodReference.Method, state);
        }
    }

    private static SemanticModel GetSemanticModel(SyntaxTree syntaxTree, JSInteropAnalyzerState state)
    {
        foreach (var operationBlock in state.BlockContext.OperationBlocks)
        {
            if (operationBlock.SemanticModel is SemanticModel semanticModel && semanticModel.SyntaxTree == syntaxTree)
            {
                return semanticModel;
            }
        }

        return state.BlockContext.Compilation.GetSemanticModel(syntaxTree);
    }

    private static void AnalyzeMethodReference(IMethodSymbol methodSymbol, JSInteropAnalyzerState state)
    {
        foreach (var syntaxRef in methodSymbol.DeclaringSyntaxReferences)
        {
            var syntaxNode = syntaxRef.GetSyntax();
            var semanticModel = GetSemanticModel(syntaxNode.SyntaxTree, state);
            var methodOperation = syntaxNode switch
            {
                MethodDeclarationSyntax methodDeclaration => semanticModel.GetOperation(methodDeclaration),
                LocalFunctionStatementSyntax localFunctionDeclaration =>
                    semanticModel.GetOperation(localFunctionDeclaration) is ILocalFunctionOperation localFunction ? localFunction.Body : null,
                _ => null,
            };
            if (methodOperation is null)
            {
                continue;
            }
            var clonedState = state.Clone();
            clonedState.CurrentDepth++;
            AnalyzeOperationsTree(methodOperation, clonedState);
        }
    }

    /// <summary>
    /// Attribute and parameter callbacks registered on the render tree run when they are invoked, not while the tree is built.
    /// Their bodies are analyzed with the flow state where they are registered, and the registration itself is not treated as a call.
    /// </summary>
    private static bool TryAnalyzeRenderTreeCallbackRegistration(IInvocationOperation invocation, JSInteropAnalyzerState state)
    {
        var targetMethod = invocation.TargetMethod;
        if ((targetMethod.Name != "AddAttribute" && targetMethod.Name != "AddComponentParameter")
            || invocation.Arguments.Length < 3
            || !SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, state.AvailableTypes[ComponentsApi.RenderTreeBuilder.MetadataName]))
        {
            return false;
        }

        if (targetMethod.Name == "AddAttribute" && IsInteractiveOnlyDomEventAttribute(invocation.Arguments[1].Value))
        {
            return true;
        }

        var callback = FindRegisteredCallback(invocation.Arguments[2].Value, state);
        if (callback is IAnonymousFunctionOperation anonymousFunction)
        {
            AnalyzeOperationsTree(anonymousFunction.Body, state.Clone());
        }
        else if (callback is IMethodReferenceOperation methodReference)
        {
            AnalyzeMethodReference(methodReference.Method, state);
        }
        else
        {
            return false;
        }

        return true;
    }

    private static IOperation? FindRegisteredCallback(IOperation valueArgument, JSInteropAnalyzerState state)
    {
        IDelegateCreationOperation? delegateCreation = null;
        if (valueArgument is IInvocationOperation invocationOperation)
        {
            if (SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ContainingType, state.AvailableTypes[ComponentsApi.EventCallbackFactory.MetadataName])
                && invocationOperation.TargetMethod.Name != "CreateBinder"
                && invocationOperation.Arguments.Length >= 2)
            {
                delegateCreation = invocationOperation.Arguments[1].Value as IDelegateCreationOperation;
            }
        }
        else if (valueArgument is IDelegateCreationOperation directDelegateCreation)
        {
            delegateCreation = directDelegateCreation;
        }
        else if (valueArgument is IConversionOperation conversionOperation
            && conversionOperation.Operand.Type is not null && conversionOperation.Operand.Type.ContainingNamespace is not null
            && conversionOperation.Operand.Type.ContainingNamespace.ToString().StartsWith(ComponentsApi.AssemblyName, StringComparison.Ordinal))
        {
            // If the value is a conversion operation, search if a delegate is created like RenderFragment or an EventCallback.
            // Multiple delegates at once shouldn't be possible.
            delegateCreation = FindFirstDelegateChild(conversionOperation.Operand);
        }

        return delegateCreation?.Target is IAnonymousFunctionOperation || delegateCreation?.Target is IMethodReferenceOperation
            ? delegateCreation.Target
            : null;
    }

    private static bool IsInteractiveOnlyDomEventAttribute(IOperation attributeNameOperation)
    {
        // Named "onsubmit" handlers are dispatched by static SSR form posts, where RendererInfo.IsInteractive is false.
        return attributeNameOperation.ConstantValue.HasValue
            && attributeNameOperation.ConstantValue.Value is string attributeName
            && attributeName.StartsWith("on", StringComparison.Ordinal)
            && !string.Equals(attributeName, "onsubmit", StringComparison.Ordinal);
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

        public Dictionary<ILocalSymbol, IOperation> StoredDelegates { get; private set; } = new Dictionary<ILocalSymbol, IOperation>();

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
                SymbolChecks = new (this.SymbolChecks),
                StoredDelegates = new (this.StoredDelegates)
            };
        }

        public void UpdateIsInteractiveChecked(bool value)
        {
            IsInteractiveChecked = IsInteractiveChecked || value;
        }
    }
}
