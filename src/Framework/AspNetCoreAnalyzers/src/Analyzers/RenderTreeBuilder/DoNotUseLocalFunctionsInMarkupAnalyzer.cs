// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

                context.RegisterOperationBlockStartAction(context =>
                {
                    if (context.OwningSymbol is not IMethodSymbol method ||
                        !Overrides(method, buildRenderTreeMethod))
                    {
                        return;
                    }

                    context.RegisterOperationAction(context =>
                    {
                        var localFunction = (ILocalFunctionOperation)context.Operation;
                        if (localFunction.Symbol.IsStatic ||
                            localFunction.Body is null ||
                            !ContainsCapturedRenderTreeBuilderCall(
                                localFunction.Body,
                                ImmutableArray.Create(localFunction.Symbol),
                                renderTreeBuilderType))
                        {
                            return;
                        }

                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup,
                            localFunction.Symbol.Locations.First(),
                            localFunction.Symbol.Name));
                    }, OperationKind.LocalFunction);
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

    private static bool ContainsCapturedRenderTreeBuilderCall(
        IOperation operation,
        ImmutableArray<IMethodSymbol> localScopes,
        INamedTypeSymbol renderTreeBuilderType)
    {
        if (operation is ILocalFunctionOperation)
        {
            return false;
        }

        if (operation is IAnonymousFunctionOperation anonymousFunction)
        {
            return ContainsCapturedRenderTreeBuilderCall(
                anonymousFunction.Body,
                localScopes.Add(anonymousFunction.Symbol),
                renderTreeBuilderType);
        }

        if (operation is IInvocationOperation invocation &&
            SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, renderTreeBuilderType) &&
            IsCaptured(invocation.Instance, localScopes))
        {
            return true;
        }

        foreach (var child in operation.ChildOperations)
        {
            if (ContainsCapturedRenderTreeBuilderCall(child, localScopes, renderTreeBuilderType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCaptured(IOperation? operation, ImmutableArray<IMethodSymbol> localScopes)
        => operation switch
        {
            IConversionOperation conversion => IsCaptured(conversion.Operand, localScopes),
            IFieldReferenceOperation => true,
            ILocalReferenceOperation local => !IsDeclaredInLocalScope(local.Local, localScopes),
            IParameterReferenceOperation parameter => !IsDeclaredInLocalScope(parameter.Parameter, localScopes),
            _ => false,
        };

    private static bool IsDeclaredInLocalScope(ISymbol symbol, ImmutableArray<IMethodSymbol> localScopes)
    {
        foreach (var localScope in localScopes)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol, localScope))
            {
                return true;
            }
        }

        return false;
    }
}
