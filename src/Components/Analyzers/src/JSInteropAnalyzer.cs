// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInteropAnalyzer : DiagnosticAnalyzer
{
    private const string JSRuntimeExtensionsTypeName = "Microsoft.JSInterop.JSRuntimeExtensions";
    private const string JSObjectReferenceExtensionsTypeName = "Microsoft.JSInterop.JSObjectReferenceExtensions";
    private const string JSInProcessRuntimeExtensionsTypeName = "Microsoft.JSInterop.JSInProcessRuntimeExtensions";
    private const string JSInProcessObjectReferenceExtensionsTypeName = "Microsoft.JSInterop.JSInProcessObjectReferenceExtensions";
    private const string IJSRuntimeTypeName = "Microsoft.JSInterop.IJSRuntime";
    private const string IJSInProcessRuntimeTypeName = "Microsoft.JSInterop.IJSInProcessRuntime";
    private const string IJSObjectReferenceTypeName = "Microsoft.JSInterop.IJSObjectReference";
    private const string IJSInProcessObjectReferenceTypeName = "Microsoft.JSInterop.IJSInProcessObjectReference";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnguardedJSInteropCall);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var ijsRuntimeType = compilationContext.Compilation.GetTypeByMetadataName(IJSRuntimeTypeName);
            var ijsInProcessRuntimeType = compilationContext.Compilation.GetTypeByMetadataName(IJSInProcessRuntimeTypeName);
            var ijsObjectReferenceType = compilationContext.Compilation.GetTypeByMetadataName(IJSObjectReferenceTypeName);
            var ijsInProcessObjectReferenceType = compilationContext.Compilation.GetTypeByMetadataName(IJSInProcessObjectReferenceTypeName);
            var jsRuntimeExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(JSRuntimeExtensionsTypeName);
            var jsObjectReferenceExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(JSObjectReferenceExtensionsTypeName);
            var jsInProcessRuntimeExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(JSInProcessRuntimeExtensionsTypeName);
            var jsInProcessObjectReferenceExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(JSInProcessObjectReferenceExtensionsTypeName);

            if (ijsRuntimeType is null &&
                ijsInProcessRuntimeType is null &&
                ijsObjectReferenceType is null &&
                ijsInProcessObjectReferenceType is null &&
                jsRuntimeExtensionsType is null &&
                jsObjectReferenceExtensionsType is null &&
                jsInProcessRuntimeExtensionsType is null &&
                jsInProcessObjectReferenceExtensionsType is null)
            {
                return;
            }

            compilationContext.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                var targetMethod = invocation.TargetMethod;

                if (IsInsideTryBlockWithCatch(invocation.Syntax))
                {
                    return;
                }

                if (!IsJSInteropInvocation(
                    invocation,
                    targetMethod,
                    ijsRuntimeType,
                    ijsInProcessRuntimeType,
                    ijsObjectReferenceType,
                    ijsInProcessObjectReferenceType,
                    jsRuntimeExtensionsType,
                    jsObjectReferenceExtensionsType,
                    jsInProcessRuntimeExtensionsType,
                    jsInProcessObjectReferenceExtensionsType))
                {
                    return;
                }

                operationContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnguardedJSInteropCall,
                    invocation.Syntax.GetLocation(),
                    targetMethod.Name));
            }, OperationKind.Invocation);
        });
    }

    private static bool IsJSInteropInvocation(
        IInvocationOperation invocation,
        IMethodSymbol targetMethod,
        INamedTypeSymbol? ijsRuntimeType,
        INamedTypeSymbol? ijsInProcessRuntimeType,
        INamedTypeSymbol? ijsObjectReferenceType,
        INamedTypeSymbol? ijsInProcessObjectReferenceType,
        INamedTypeSymbol? jsRuntimeExtensionsType,
        INamedTypeSymbol? jsObjectReferenceExtensionsType,
        INamedTypeSymbol? jsInProcessRuntimeExtensionsType,
        INamedTypeSymbol? jsInProcessObjectReferenceExtensionsType)
    {
        if (IsJSInteropType(targetMethod.ContainingType, ijsRuntimeType, ijsInProcessRuntimeType, ijsObjectReferenceType, ijsInProcessObjectReferenceType))
        {
            return true;
        }

        if (!IsJSInteropExtensionClass(targetMethod.ContainingType, jsRuntimeExtensionsType, jsObjectReferenceExtensionsType, jsInProcessRuntimeExtensionsType, jsInProcessObjectReferenceExtensionsType))
        {
            return false;
        }

        var receiverType = GetReceiverType(invocation);
        return IsJSInteropType(receiverType, ijsRuntimeType, ijsInProcessRuntimeType, ijsObjectReferenceType, ijsInProcessObjectReferenceType);
    }

    private static bool IsInsideTryBlockWithCatch(SyntaxNode invocationSyntax)
    {
        foreach (var tryStatement in invocationSyntax.Ancestors().OfType<TryStatementSyntax>())
        {
            if (tryStatement.Catches.Count > 0 && tryStatement.Block.Span.Contains(invocationSyntax.Span))
            {
                return true;
            }
        }

        return false;
    }

    private static ITypeSymbol? GetReceiverType(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
        {
            return invocation.Arguments[0].Value.Type;
        }

        return invocation.Instance?.Type;
    }

    private static bool IsJSInteropType(
        ITypeSymbol? type,
        INamedTypeSymbol? ijsRuntimeType,
        INamedTypeSymbol? ijsInProcessRuntimeType,
        INamedTypeSymbol? ijsObjectReferenceType,
        INamedTypeSymbol? ijsInProcessObjectReferenceType)
    {
        if (type is null)
        {
            return false;
        }

        if (ImplementsInterface(type, ijsRuntimeType, IJSRuntimeTypeName))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsInProcessRuntimeType, IJSInProcessRuntimeTypeName))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsObjectReferenceType, IJSObjectReferenceTypeName))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsInProcessObjectReferenceType, IJSInProcessObjectReferenceTypeName))
        {
            return true;
        }

        return false;
    }

    private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceType, string interfaceTypeName)
    {
        if (interfaceType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, interfaceType))
            {
                return true;
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, interfaceType))
                {
                    return true;
                }
            }

            return false;
        }

        if (type is INamedTypeSymbol namedType && namedType.ToDisplayString() == interfaceTypeName)
        {
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == interfaceTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsJSInteropExtensionClass(
        INamedTypeSymbol containingType,
        INamedTypeSymbol? jsRuntimeExtensionsType,
        INamedTypeSymbol? jsObjectReferenceExtensionsType,
        INamedTypeSymbol? jsInProcessRuntimeExtensionsType,
        INamedTypeSymbol? jsInProcessObjectReferenceExtensionsType)
    {
        return SymbolEqualityComparer.Default.Equals(containingType, jsRuntimeExtensionsType) ||
               SymbolEqualityComparer.Default.Equals(containingType, jsObjectReferenceExtensionsType) ||
               SymbolEqualityComparer.Default.Equals(containingType, jsInProcessRuntimeExtensionsType) ||
               SymbolEqualityComparer.Default.Equals(containingType, jsInProcessObjectReferenceExtensionsType);
    }
}
