// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private IMethodSymbol? _currentLocalFunction;

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

        public override void VisitConditional(IConditionalOperation operation)
        {
            Visit(operation.Condition);
            var initialProvenance = CloneProvenance();

            Visit(operation.WhenTrue);
            var whenTrueProvenance = CloneProvenance();

            RestoreProvenance(initialProvenance);
            if (operation.WhenFalse is { } whenFalse)
            {
                Visit(whenFalse);
            }

            MergeProvenance(whenTrueProvenance);
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
    }
}
