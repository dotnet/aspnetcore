// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that detects usage of Virtualize with ItemsProvider but without ItemComparer.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VirtualizeItemComparerAnalyzer : DiagnosticAnalyzer
{
    private const string VirtualizeTypeName = "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize`1";
    private const string RenderTreeBuilderTypeName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.VirtualizeItemsProviderRequiresItemComparer);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var virtualizeType = compilationContext.Compilation.GetTypeByMetadataName(VirtualizeTypeName);
            var renderTreeBuilderType = compilationContext.Compilation.GetTypeByMetadataName(RenderTreeBuilderTypeName);

            if (virtualizeType is null || renderTreeBuilderType is null)
            {
                return;
            }

            compilationContext.RegisterOperationBlockStartAction(blockContext =>
            {
                var componentStack = new Stack<ComponentState>();
                var completedVirtualizeComponents = new List<ComponentState>();

                blockContext.RegisterOperationAction(operationContext =>
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    var targetMethod = invocation.TargetMethod;

                    if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, renderTreeBuilderType))
                    {
                        return;
                    }

                    switch (targetMethod.Name)
                    {
                        case "OpenComponent":
                            if (targetMethod.IsGenericMethod && targetMethod.TypeArguments.Length == 1)
                            {
                                var typeArg = targetMethod.TypeArguments[0];
                                var originalDef = typeArg is INamedTypeSymbol namedType && namedType.IsGenericType
                                    ? namedType.OriginalDefinition
                                    : typeArg;

                                if (SymbolEqualityComparer.Default.Equals(originalDef, virtualizeType))
                                {
                                    componentStack.Push(new ComponentState { IsVirtualize = true });
                                }
                                else
                                {
                                    componentStack.Push(new ComponentState { IsVirtualize = false });
                                }
                            }
                            else
                            {
                                componentStack.Push(new ComponentState { IsVirtualize = false });
                            }
                            break;

                        case "AddComponentParameter":
                            if (componentStack.Count > 0 && componentStack.Peek().IsVirtualize)
                            {
                                if (invocation.Arguments.Length >= 2)
                                {
                                    var nameArg = invocation.Arguments[1];
                                    if (nameArg.Value.ConstantValue.HasValue &&
                                        nameArg.Value.ConstantValue.Value is string paramName)
                                    {
                                        var state = componentStack.Peek();
                                        if (paramName == "ItemsProvider")
                                        {
                                            state.HasItemsProvider = true;
                                            state.ItemsProviderLocation = invocation.Syntax.GetLocation();
                                        }
                                        else if (paramName == "ItemComparer")
                                        {
                                            state.HasItemComparer = true;
                                        }
                                    }
                                }
                            }
                            break;

                        case "CloseComponent":
                            if (componentStack.Count > 0)
                            {
                                var state = componentStack.Pop();
                                if (state.IsVirtualize)
                                {
                                    completedVirtualizeComponents.Add(state);
                                }
                            }
                            break;
                    }
                }, OperationKind.Invocation);

                blockContext.RegisterOperationBlockEndAction(endContext =>
                {
                    foreach (var state in completedVirtualizeComponents)
                    {
                        if (state.HasItemsProvider && !state.HasItemComparer && state.ItemsProviderLocation != null)
                        {
                            endContext.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.VirtualizeItemsProviderRequiresItemComparer,
                                state.ItemsProviderLocation));
                        }
                    }
                });
            });
        });
    }

    private sealed class ComponentState
    {
        public bool IsVirtualize { get; set; }
        public bool HasItemsProvider { get; set; }
        public bool HasItemComparer { get; set; }
        public Location? ItemsProviderLocation { get; set; }
    }
}
