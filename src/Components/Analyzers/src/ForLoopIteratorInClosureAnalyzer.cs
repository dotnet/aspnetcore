// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForLoopIteratorInClosureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure);

    public override void Initialize(AnalysisContext initContext)
    {
        initContext.EnableConcurrentExecution();
        initContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        initContext.RegisterCompilationStartAction(compilationContext =>
        {
            var availableTypes = new Dictionary<string, INamedTypeSymbol?>()
            {
                {
                    ComponentsApi.BindConverter.MetadataName,
                    compilationContext.Compilation.GetTypeByMetadataName(ComponentsApi.BindConverter.FullTypeName)
                },
                {
                    ComponentsApi.ComponentBase.MetadataName,
                    compilationContext.Compilation.GetTypeByMetadataName(ComponentsApi.ComponentBase.FullTypeName)
                },
                {
                    ComponentsApi.EventCallbackFactory.MetadataName,
                    compilationContext.Compilation.GetTypeByMetadataName(ComponentsApi.EventCallbackFactory.FullTypeName)
                },
                {
                    ComponentsApi.RenderTreeBuilder.MetadataName,
                    compilationContext.Compilation.GetTypeByMetadataName(ComponentsApi.RenderTreeBuilder.FullTypeName)
                },
            };
            if (availableTypes[ComponentsApi.ComponentBase.MetadataName] is null
                || availableTypes[ComponentsApi.RenderTreeBuilder.MetadataName] is null)
            {
                return;
            }

            compilationContext.RegisterOperationBlockStartAction(blockContext =>
            {
                var analyzerState = new ForLoopAnalyzerState();
                if (blockContext.OwningSymbol is IMethodSymbol owningMethod
                    && IsImplementationOfBuildRenderTree(owningMethod, availableTypes))
                {
                    // Register variables from the initialization of for loops.
                    blockContext.RegisterOperationAction(context => AnalyzeForLoopVariables(context, analyzerState),
                        OperationKind.Loop);

                    // Check if non-incremented variables are later incremented/set.
                    blockContext.RegisterOperationAction(context => AnalyzeIncrementOrDecrement(context, analyzerState),
                        OperationKind.Increment);
                    blockContext.RegisterOperationAction(context => AnalyzeIncrementOrDecrement(context, analyzerState),
                        OperationKind.Decrement);
                    blockContext.RegisterOperationAction(context => AnalyzeAssignment(context, analyzerState),
                        OperationKind.SimpleAssignment);
                    blockContext.RegisterOperationAction(context => AnalyzeAssignment(context, analyzerState),
                        OperationKind.CompoundAssignment);

                    // Main analysis for invocations of AddAttribute and AddComponentParameter.
                    blockContext.RegisterOperationAction(context =>
                    {
                        // It appears that the analysis triggers the actions for operations using DFS.
                        // The only uncertainty seems to be for actions on the same level, but we don't need to worry about them.(probably due to foreach)
                        // We care only for the upper levels variables to be registered in time of execution of this callback, so we catch any occurrences.
                        AnalyzeInvocation(context, availableTypes, analyzerState);
                    }, OperationKind.Invocation);
                }
                else
                {
                    return;
                }

                blockContext.RegisterOperationBlockEndAction(endContext =>
                {
                    analyzerState.ReportDiagnostics(endContext);
                });
            });
        });
    }

    private static bool IsImplementationOfBuildRenderTree(IMethodSymbol methodSymbol, Dictionary<string, INamedTypeSymbol?> availableTypes)
    {
        if (methodSymbol.Name != "BuildRenderTree" || !methodSymbol.IsOverride)
        {
            return false;
        }
        var containingType = methodSymbol.ContainingType;
        while (containingType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(containingType, availableTypes[ComponentsApi.ComponentBase.MetadataName]))
            {
                return true;
            }
            containingType = containingType.BaseType;
        }
        return false;
    }

    private static void AnalyzeForLoopVariables(OperationAnalysisContext operationContext, ForLoopAnalyzerState analyzerState)
    {
        if (operationContext.Operation is IForLoopOperation forLoopOperation)
        {
            // Get all incremented variables.
            foreach (var bottomOperation in forLoopOperation.AtLoopBottom)
            {
                if (bottomOperation is IExpressionStatementOperation expression)
                {
                    if (expression.Operation is IIncrementOrDecrementOperation operation
                        && operation.Target is ILocalReferenceOperation target)
                    {
                        analyzerState.AddIterator(target.Local);
                    }
                    else if (expression.Operation is IAssignmentOperation assignment
                        && assignment.Target is ILocalReferenceOperation assignmentTarget)
                    {
                        analyzerState.AddIterator(assignmentTarget.Local);
                    }
                }
            }

            // The rest add as potentials.
            foreach (var localVar in forLoopOperation.Locals)
            {
                analyzerState.AddPotentialIterator(localVar);
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, Dictionary<string, INamedTypeSymbol?> availableTypes, ForLoopAnalyzerState analyzerState)
    {
        if (analyzerState.AllForVariables.Count == 0)
        {
            return;
        }

        if (context.Operation is IInvocationOperation invocation
            && invocation.Instance?.Type is not null
            && SymbolEqualityComparer.Default.Equals(invocation.Instance.Type, availableTypes[ComponentsApi.RenderTreeBuilder.MetadataName])
            && invocation.Arguments.Length >= 3)
        {
            var targetMethod = invocation.TargetMethod;
            if (targetMethod.Name != "AddAttribute" && targetMethod.Name != "AddComponentParameter")
            {
                return;
            }

            // Get the operation that when containing variable reference could cause an issue. Usually an anonymous function or @bind
            IOperation? suspectOperation = null;
            var valueArgument = invocation.Arguments[2].Value;
            if (valueArgument is IInvocationOperation valueInvocation)
            {
                if (SymbolEqualityComparer.Default.Equals(valueInvocation.TargetMethod.ContainingType, availableTypes[ComponentsApi.BindConverter.MetadataName]))
                {
                    // Use the 'value' attribute setter when using @bind, for the location to be more accurate when reporting.
                    suspectOperation = valueInvocation;
                }
                else if (SymbolEqualityComparer.Default.Equals(valueInvocation.TargetMethod.ContainingType, availableTypes[ComponentsApi.EventCallbackFactory.MetadataName])
                    && valueInvocation.TargetMethod.Name != "CreateBinder"
                    && valueInvocation.Arguments.Length >= 2
                    && valueInvocation.Arguments[1].Value is IDelegateCreationOperation delegateCreation
                    && delegateCreation.Target is IAnonymousFunctionOperation)
                {
                    // The anonymous function of a basic event callback.
                    suspectOperation = delegateCreation.Target;
                }
            }
            else if (valueArgument is IDelegateCreationOperation delegateCreation
                && delegateCreation.Target is IAnonymousFunctionOperation)
            {
                suspectOperation = delegateCreation.Target;
            }
            else if (valueArgument is IConversionOperation conversionOperation
                && conversionOperation.Operand.Type is not null
                && conversionOperation.Operand.Type.ContainingNamespace.ToString().StartsWith(ComponentsApi.AssemblyName, StringComparison.Ordinal))
            {
                // If the value is a conversion operation, search if a delegate is created like RenderFragment or an EventCallback.
                // Multiple delegates at once shouldn't be possible.
                var delegateResult = FindFirstDelegateChild(conversionOperation.Operand);
                if (delegateResult is not null && delegateResult.Target is IAnonymousFunctionOperation)
                {
                    suspectOperation = delegateResult.Target;
                }
            }

            if (suspectOperation is not null)
            {
                var usedVariables = suspectOperation.Descendants().OfType<ILocalReferenceOperation>();
                analyzerState.AddRelatedOccurrences(usedVariables);
            }
        }
    }

    private static void AnalyzeIncrementOrDecrement(OperationAnalysisContext context, ForLoopAnalyzerState analyzerState)
    {
        if (context.Operation is IIncrementOrDecrementOperation incrementOrDecrement
            && incrementOrDecrement.Target is ILocalReferenceOperation localReference
            && analyzerState.IsPotentialIterator(localReference.Local))
        {
            analyzerState.OnPotentialIteratorChanged(localReference.Local);
        }
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context, ForLoopAnalyzerState analyzerState)
    {
        if (context.Operation is IAssignmentOperation assignment
            && assignment.Target is ILocalReferenceOperation localReference
            && analyzerState.IsPotentialIterator(localReference.Local))
        {
            analyzerState.OnPotentialIteratorChanged(localReference.Local);
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
            foreach (var child in currentOperation.Children)
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

    /// <summary>
    /// State to keep track of variables that are being incremented or potentially incremented and their occurrences in the current for loop.
    /// </summary>
    private sealed class ForLoopAnalyzerState
    {
        // Variables that are being incremented on in the current context. 
        public List<ILocalSymbol> Iterators { get; } = new();
        // Potential variable that are not yet being incremented on which would not cause any issues so far.
        public List<ILocalSymbol> PotentialIterators { get; private set; } = new();

        // All variable that are being used in the current context. 
        public List<ILocalSymbol> AllForVariables { get; } = new();

        // Occurrences of potential variables that could be later on incremented. Not inherited from parent state.
        public List<ILocalReferenceOperation> IteratorOccurrences { get; } = new();

        public bool IsIterator(ILocalSymbol target)
        {
            return Iterators.Any(existing => SymbolEqualityComparer.Default.Equals(existing, target));
        }

        public bool IsPotentialIterator(ILocalSymbol? target)
        {
            return target is not null && PotentialIterators.Any(existing => SymbolEqualityComparer.Default.Equals(existing, target));
        }

        public void AddIterator(ILocalSymbol iterator)
        {
            if (!IsIterator(iterator))
            {
                Iterators.Add(iterator);
                AllForVariables.Add(iterator);
            }
        }

        public void AddPotentialIterator(ILocalSymbol potentialIterator)
        {
            if (!IsIterator(potentialIterator) && !IsPotentialIterator(potentialIterator))
            {
                PotentialIterators.Add(potentialIterator);
                AllForVariables.Add(potentialIterator);
            }
        }

        /// <summary>
        /// When potential variable is incremented, move it to incremented variables and report diagnostics for all previous occurrences of it in the current context.
        /// </summary>
        public void OnPotentialIteratorChanged(ILocalSymbol potentialIterator)
        {
            Iterators.Add(potentialIterator);
            PotentialIterators = PotentialIterators.Where(name => !SymbolEqualityComparer.Default.Equals(name, potentialIterator)).ToList();
        }

        public void AddRelatedOccurrences(IEnumerable<ILocalReferenceOperation> variableReferences)
        {
            foreach (var variableReference in variableReferences)
            {
                if (AllForVariables.Any(forVariable => SymbolEqualityComparer.Default.Equals(forVariable, variableReference.Local)))
                {
                    IteratorOccurrences.Add(variableReference);
                }
            }
        }

        public void ReportDiagnostics(OperationBlockAnalysisContext context)
        {
            var referencesToReport = IteratorOccurrences.Where(reference => IsIterator(reference.Local));
            foreach (var reference in referencesToReport)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure,
                    reference.Syntax.GetLocation(),
                    reference.Local.Name));
            }
        }
    }
}
