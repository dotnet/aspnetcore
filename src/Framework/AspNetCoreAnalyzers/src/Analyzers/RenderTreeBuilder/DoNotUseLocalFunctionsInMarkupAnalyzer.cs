// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseLocalFunctionsInMarkupAnalyzer : DiagnosticAnalyzer
{
    private const string BuildRenderTreeMethodName = "BuildRenderTree";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
            var componentBaseType = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
            var renderTreeBuilderType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Components_Rendering_RenderTreeBuilder);
            var buildRenderTreeMethod = componentBaseType?
                .GetMembers(BuildRenderTreeMethodName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method =>
                    method.Parameters.Length == 1 &&
                    SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, renderTreeBuilderType));
            if (componentBaseType is null || renderTreeBuilderType is null || buildRenderTreeMethod is null)
            {
                return;
            }

            context.RegisterSymbolStartAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;
                if (!InheritsFromComponentBase(type, componentBaseType))
                {
                    return;
                }

                context.RegisterOperationBlockAction(context =>
                {
                    if (context.OwningSymbol is not IMethodSymbol method ||
                        !Overrides(method, buildRenderTreeMethod))
                    {
                        return;
                    }

                    var localFunctions = new Dictionary<IMethodSymbol, ILocalFunctionOperation>(SymbolEqualityComparer.Default);
                    foreach (var operationBlock in context.OperationBlocks)
                    {
                        CollectLocalFunctions(operationBlock, localFunctions);
                    }

                    var walker = new OwningBuilderWalker(method.Parameters[0], renderTreeBuilderType, localFunctions);
                    foreach (var operationBlock in context.OperationBlocks)
                    {
                        walker.Visit(operationBlock);
                    }

                    foreach (var localFunction in walker.LocalFunctionsUsingOwningBuilder)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup,
                            localFunction.Locations.First(),
                            localFunction.Name));
                    }
                });
            }, SymbolKind.NamedType);
        });
    }

    private static bool InheritsFromComponentBase(INamedTypeSymbol type, INamedTypeSymbol componentBaseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, componentBaseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Overrides(IMethodSymbol method, IMethodSymbol overriddenMethod)
    {
        for (var current = method; current is not null; current = current.OverriddenMethod)
        {
            if (SymbolEqualityComparer.Default.Equals(current, overriddenMethod))
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectLocalFunctions(
        IOperation operation,
        Dictionary<IMethodSymbol, ILocalFunctionOperation> localFunctions)
    {
        if (operation is ILocalFunctionOperation localFunction)
        {
            localFunctions.Add(localFunction.Symbol, localFunction);
        }

        foreach (var child in operation.ChildOperations)
        {
            CollectLocalFunctions(child, localFunctions);
        }
    }

    private sealed class OwningBuilderWalker : OperationWalker
    {
        private readonly INamedTypeSymbol _renderTreeBuilderType;
        private readonly Dictionary<IMethodSymbol, ILocalFunctionOperation> _localFunctions;
        private readonly HashSet<IMethodSymbol> _activeLocalFunctions = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<ISymbol, bool> _provenance = new(SymbolEqualityComparer.Default);
        private readonly Stack<LoopContext> _loopContexts = new();
        private IMethodSymbol? _currentLocalFunction;
        private bool _pathTerminated;

        public OwningBuilderWalker(
            IParameterSymbol owningBuilder,
            INamedTypeSymbol renderTreeBuilderType,
            Dictionary<IMethodSymbol, ILocalFunctionOperation> localFunctions)
        {
            _renderTreeBuilderType = renderTreeBuilderType;
            _localFunctions = localFunctions;
            _provenance.Add(owningBuilder, true);
        }

        public HashSet<IMethodSymbol> LocalFunctionsUsingOwningBuilder { get; } = new(SymbolEqualityComparer.Default);

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
        }

        public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
        {
            var previousProvenance = CloneProvenance();
            foreach (var parameter in operation.Symbol.Parameters)
            {
                _provenance[parameter] = false;
            }

            Visit(operation.Body);
            RestoreProvenance(previousProvenance);
        }

        public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
        {
            if (operation.Initializer is { } initializer)
            {
                Visit(initializer.Value);
                _provenance[operation.Symbol] = HasOwningBuilderProvenance(initializer.Value);
            }
            else
            {
                _provenance[operation.Symbol] = false;
            }
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            Visit(operation.Value);
            if (GetReferencedSymbol(operation.Target) is { } target)
            {
                _provenance[target] = HasOwningBuilderProvenance(operation.Value);
            }
        }

        public override void VisitBlock(IBlockOperation operation)
        {
            foreach (var child in operation.Operations)
            {
                Visit(child);
                if (_pathTerminated)
                {
                    break;
                }
            }
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            var correspondingOperation = operation.GetCorrespondingOperation();
            foreach (var loopContext in _loopContexts)
            {
                if (!ReferenceEquals(correspondingOperation, loopContext.Operation))
                {
                    continue;
                }

                switch (operation.BranchKind)
                {
                    case BranchKind.Break:
                        loopContext.BreakStates.Add(CloneProvenance());
                        _pathTerminated = true;
                        break;
                    case BranchKind.Continue:
                        loopContext.ContinueStates.Add(CloneProvenance());
                        _pathTerminated = true;
                        break;
                }

                return;
            }
        }

        public override void VisitConditional(IConditionalOperation operation)
        {
            Visit(operation.Condition);
            var initialProvenance = CloneProvenance();

            _pathTerminated = false;
            Visit(operation.WhenTrue);
            var whenTrueProvenance = CloneProvenance();
            var whenTrueTerminated = _pathTerminated;

            RestoreProvenance(initialProvenance);
            _pathTerminated = false;
            if (operation.WhenFalse is { } whenFalse)
            {
                Visit(whenFalse);
            }

            var whenFalseProvenance = CloneProvenance();
            var whenFalseTerminated = _pathTerminated;
            if (whenTrueTerminated && whenFalseTerminated)
            {
                _pathTerminated = true;
            }
            else if (whenTrueTerminated)
            {
                RestoreProvenance(whenFalseProvenance);
                _pathTerminated = false;
            }
            else
            {
                RestoreProvenance(whenTrueProvenance);
                if (!whenFalseTerminated)
                {
                    MergeProvenance(whenFalseProvenance);
                }

                _pathTerminated = false;
            }
        }

        public override void VisitSwitch(ISwitchOperation operation)
        {
            Visit(operation.Value);
            var initialProvenance = CloneProvenance();
            var mergedProvenance = CloneProvenance();

            foreach (var @case in operation.Cases)
            {
                RestoreProvenance(initialProvenance);
                _pathTerminated = false;
                Visit(@case);
                if (_pathTerminated)
                {
                    continue;
                }

                var caseProvenance = CloneProvenance();

                RestoreProvenance(mergedProvenance);
                MergeProvenance(caseProvenance);
                mergedProvenance = CloneProvenance();
            }

            RestoreProvenance(mergedProvenance);
            _pathTerminated = false;
        }

        public override void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            foreach (var clause in operation.Clauses)
            {
                Visit(clause);
            }

            foreach (var child in operation.Body)
            {
                Visit(child);
                if (_pathTerminated)
                {
                    break;
                }
            }
        }

        public override void VisitWhileLoop(IWhileLoopOperation operation)
        {
            if (operation.ConditionIsTop)
            {
                VisitLoop(
                    operation,
                    () =>
                    {
                        Visit(operation.Condition);
                        if (!_pathTerminated)
                        {
                            Visit(operation.Body);
                        }
                    },
                    visitContinue: null,
                    () => Visit(operation.Condition),
                    executesAtLeastOnce: false);
            }
            else
            {
                VisitLoop(
                    operation,
                    () =>
                    {
                        Visit(operation.Body);
                        if (!_pathTerminated)
                        {
                            Visit(operation.Condition);
                        }
                    },
                    () => Visit(operation.Condition),
                    visitExit: null,
                    executesAtLeastOnce: true);
            }
        }

        public override void VisitForLoop(IForLoopOperation operation)
        {
            foreach (var before in operation.Before)
            {
                Visit(before);
            }

            VisitLoop(
                operation,
                () =>
                {
                    Visit(operation.Condition);
                    if (!_pathTerminated)
                    {
                        Visit(operation.Body);
                    }

                    if (!_pathTerminated)
                    {
                        VisitForLoopBottom(operation);
                    }
                },
                () => VisitForLoopBottom(operation),
                () => Visit(operation.Condition),
                executesAtLeastOnce: false);
        }

        public override void VisitForEachLoop(IForEachLoopOperation operation)
        {
            Visit(operation.Collection);
            VisitLoop(
                operation,
                () =>
                {
                    Visit(operation.LoopControlVariable);
                    if (!_pathTerminated)
                    {
                        Visit(operation.Body);
                    }

                    if (!_pathTerminated)
                    {
                        VisitForEachLoopBottom(operation);
                    }
                },
                () => VisitForEachLoopBottom(operation),
                visitExit: null,
                executesAtLeastOnce: false);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            Visit(operation.Instance);
            foreach (var argument in operation.Arguments)
            {
                Visit(argument.Value);
            }

            if (_currentLocalFunction is not null &&
                SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, _renderTreeBuilderType) &&
                HasOwningBuilderProvenance(operation.Instance))
            {
                LocalFunctionsUsingOwningBuilder.Add(_currentLocalFunction);
            }

            if (_localFunctions.TryGetValue(operation.TargetMethod, out var localFunction))
            {
                VisitLocalFunctionInvocation(localFunction);
            }
        }

        public override void VisitMethodReference(IMethodReferenceOperation operation)
        {
            Visit(operation.Instance);
            if (_localFunctions.TryGetValue(operation.Method, out var localFunction))
            {
                VisitLocalFunctionInvocation(localFunction);
            }
        }

        private void VisitLocalFunctionInvocation(ILocalFunctionOperation localFunction)
        {
            if (localFunction.Symbol.IsStatic ||
                localFunction.Body is null ||
                !_activeLocalFunctions.Add(localFunction.Symbol))
            {
                return;
            }

            var previousLocalFunction = _currentLocalFunction;
            _currentLocalFunction = localFunction.Symbol;
            foreach (var parameter in localFunction.Symbol.Parameters)
            {
                _provenance[parameter] = false;
            }

            Visit(localFunction.Body);

            _currentLocalFunction = previousLocalFunction;
            _activeLocalFunctions.Remove(localFunction.Symbol);
        }

        private void VisitForLoopBottom(IForLoopOperation operation)
        {
            foreach (var atLoopBottom in operation.AtLoopBottom)
            {
                Visit(atLoopBottom);
            }
        }

        private void VisitForEachLoopBottom(IForEachLoopOperation operation)
        {
            foreach (var nextVariable in operation.NextVariables)
            {
                Visit(nextVariable);
            }
        }

        private void VisitLoop(
            ILoopOperation operation,
            Action visitIteration,
            Action? visitContinue,
            Action? visitExit,
            bool executesAtLeastOnce)
        {
            var loopContext = new LoopContext(operation);
            _loopContexts.Push(loopContext);
            try
            {
                Dictionary<ISymbol, bool>? loopStates;
                if (executesAtLeastOnce)
                {
                    loopStates = VisitLoopIteration(loopContext, visitIteration, visitContinue);
                }
                else
                {
                    loopStates = CloneProvenance();
                }

                while (loopStates is not null)
                {
                    RestoreProvenance(loopStates);
                    var iterationEnd = VisitLoopIteration(loopContext, visitIteration, visitContinue);
                    var mergedStates = MergeProvenance(loopStates, iterationEnd)!;
                    if (HasSameProvenance(loopStates, mergedStates))
                    {
                        loopStates = mergedStates;
                        break;
                    }

                    loopStates = mergedStates;
                }

                Dictionary<ISymbol, bool>? exitStates = null;
                if (loopStates is not null)
                {
                    RestoreProvenance(loopStates);
                    _pathTerminated = false;
                    visitExit?.Invoke();
                    if (!_pathTerminated)
                    {
                        exitStates = CloneProvenance();
                    }
                }

                exitStates = MergeProvenance(exitStates, MergeProvenance(loopContext.BreakStates));
                if (exitStates is not null)
                {
                    RestoreProvenance(exitStates);
                }

                _pathTerminated = exitStates is null;
            }
            finally
            {
                _loopContexts.Pop();
            }
        }

        private Dictionary<ISymbol, bool>? VisitLoopIteration(
            LoopContext loopContext,
            Action visitIteration,
            Action? visitContinue)
        {
            loopContext.ContinueStates.Clear();
            _pathTerminated = false;
            visitIteration();

            var iterationStates = new List<Dictionary<ISymbol, bool>>();
            if (!_pathTerminated)
            {
                iterationStates.Add(CloneProvenance());
            }

            foreach (var continueState in loopContext.ContinueStates)
            {
                RestoreProvenance(continueState);
                _pathTerminated = false;
                visitContinue?.Invoke();
                if (!_pathTerminated)
                {
                    iterationStates.Add(CloneProvenance());
                }
            }

            _pathTerminated = false;
            return MergeProvenance(iterationStates);
        }

        private bool HasOwningBuilderProvenance(IOperation? operation)
            => operation switch
            {
                IConversionOperation conversion => HasOwningBuilderProvenance(conversion.Operand),
                IParenthesizedOperation parenthesized => HasOwningBuilderProvenance(parenthesized.Operand),
                ILocalReferenceOperation local => GetProvenance(local.Local),
                IParameterReferenceOperation parameter => GetProvenance(parameter.Parameter),
                IFieldReferenceOperation field => GetProvenance(field.Field),
                IConditionalOperation conditional => HasOwningBuilderProvenance(conditional.WhenTrue) ||
                    HasOwningBuilderProvenance(conditional.WhenFalse),
                ICoalesceOperation coalesce => HasOwningBuilderProvenance(coalesce.Value) ||
                    HasOwningBuilderProvenance(coalesce.WhenNull),
                ISimpleAssignmentOperation assignment => HasOwningBuilderProvenance(assignment.Value),
                _ => false,
            };

        private bool GetProvenance(ISymbol symbol)
            => _provenance.TryGetValue(symbol, out var hasOwningBuilderProvenance) &&
                hasOwningBuilderProvenance;

        private static ISymbol? GetReferencedSymbol(IOperation operation)
            => operation switch
            {
                IConversionOperation conversion => GetReferencedSymbol(conversion.Operand),
                IParenthesizedOperation parenthesized => GetReferencedSymbol(parenthesized.Operand),
                ILocalReferenceOperation local => local.Local,
                IParameterReferenceOperation parameter => parameter.Parameter,
                IFieldReferenceOperation field => field.Field,
                _ => null,
            };

        private Dictionary<ISymbol, bool> CloneProvenance()
            => new(_provenance, SymbolEqualityComparer.Default);

        private void RestoreProvenance(Dictionary<ISymbol, bool> provenance)
        {
            _provenance.Clear();
            foreach (var item in provenance)
            {
                _provenance.Add(item.Key, item.Value);
            }
        }

        private void MergeProvenance(Dictionary<ISymbol, bool> provenance)
        {
            foreach (var item in provenance)
            {
                if (item.Value)
                {
                    _provenance[item.Key] = true;
                }
            }
        }

        private static Dictionary<ISymbol, bool>? MergeProvenance(
            Dictionary<ISymbol, bool>? left,
            Dictionary<ISymbol, bool>? right)
        {
            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }

            var merged = new Dictionary<ISymbol, bool>(left, SymbolEqualityComparer.Default);
            foreach (var item in right)
            {
                if (item.Value)
                {
                    merged[item.Key] = true;
                }
            }

            return merged;
        }

        private static Dictionary<ISymbol, bool>? MergeProvenance(
            IEnumerable<Dictionary<ISymbol, bool>> states)
        {
            Dictionary<ISymbol, bool>? merged = null;
            foreach (var state in states)
            {
                merged = MergeProvenance(merged, state);
            }

            return merged;
        }

        private static bool HasSameProvenance(
            Dictionary<ISymbol, bool> left,
            Dictionary<ISymbol, bool> right)
            => left.All(item => !item.Value || GetProvenance(right, item.Key)) &&
                right.All(item => !item.Value || GetProvenance(left, item.Key));

        private static bool GetProvenance(Dictionary<ISymbol, bool> provenance, ISymbol symbol)
            => provenance.TryGetValue(symbol, out var hasOwningBuilderProvenance) &&
                hasOwningBuilderProvenance;

        private sealed class LoopContext
        {
            public LoopContext(ILoopOperation operation)
            {
                Operation = operation;
            }

            public ILoopOperation Operation { get; }

            public List<Dictionary<ISymbol, bool>> BreakStates { get; } = [];

            public List<Dictionary<ISymbol, bool>> ContinueStates { get; } = [];
        }
    }
}
