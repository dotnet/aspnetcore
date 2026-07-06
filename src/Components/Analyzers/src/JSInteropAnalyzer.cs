// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
    private const string ExceptionTypeName = "System.Exception";
    private const string InvalidOperationExceptionTypeName = "System.InvalidOperationException";
    private const string JSExceptionTypeName = "Microsoft.JSInterop.JSException";
    private const string JSDisconnectedExceptionTypeName = "Microsoft.JSInterop.JSDisconnectedException";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnguardedJSInteropCall);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var ijsRuntimeType = context.Compilation.GetTypeByMetadataName(IJSRuntimeTypeName);
            var ijsInProcessRuntimeType = context.Compilation.GetTypeByMetadataName(IJSInProcessRuntimeTypeName);
            var ijsObjectReferenceType = context.Compilation.GetTypeByMetadataName(IJSObjectReferenceTypeName);
            var ijsInProcessObjectReferenceType = context.Compilation.GetTypeByMetadataName(IJSInProcessObjectReferenceTypeName);
            var jsRuntimeExtensionsType = context.Compilation.GetTypeByMetadataName(JSRuntimeExtensionsTypeName);
            var jsObjectReferenceExtensionsType = context.Compilation.GetTypeByMetadataName(JSObjectReferenceExtensionsTypeName);
            var jsInProcessRuntimeExtensionsType = context.Compilation.GetTypeByMetadataName(JSInProcessRuntimeExtensionsTypeName);
            var jsInProcessObjectReferenceExtensionsType = context.Compilation.GetTypeByMetadataName(JSInProcessObjectReferenceExtensionsTypeName);
            var systemExceptionType = context.Compilation.GetTypeByMetadataName(ExceptionTypeName);
            var invalidOperationExceptionType = context.Compilation.GetTypeByMetadataName(InvalidOperationExceptionTypeName);
            var jsExceptionType = context.Compilation.GetTypeByMetadataName(JSExceptionTypeName);
            var jsDisconnectedExceptionType = context.Compilation.GetTypeByMetadataName(JSDisconnectedExceptionTypeName);

            var knownJsInteropExceptionTypesBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(3);

            if (invalidOperationExceptionType is not null)
            {
                knownJsInteropExceptionTypesBuilder.Add(invalidOperationExceptionType);
            }

            if (jsExceptionType is not null)
            {
                knownJsInteropExceptionTypesBuilder.Add(jsExceptionType);
            }

            if (jsDisconnectedExceptionType is not null)
            {
                knownJsInteropExceptionTypesBuilder.Add(jsDisconnectedExceptionType);
            }

            var knownJsInteropExceptionTypes = knownJsInteropExceptionTypesBuilder.ToImmutable();

            if (ijsRuntimeType is null &&
                ijsInProcessRuntimeType is null &&
                ijsObjectReferenceType is null &&
                ijsInProcessObjectReferenceType is null &&
                jsRuntimeExtensionsType is null &&
                jsObjectReferenceExtensionsType is null &&
                jsInProcessRuntimeExtensionsType is null &&
                jsInProcessObjectReferenceExtensionsType is null &&
                systemExceptionType is null &&
                knownJsInteropExceptionTypes.IsEmpty)
            {
                return;
            }

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;
                var targetMethod = invocation.TargetMethod;

                if (IsInsideTryBlockWithAllowedExceptionHandling(invocation, systemExceptionType, knownJsInteropExceptionTypes))
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

                context.ReportDiagnostic(Diagnostic.Create(
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

    private static bool IsInsideTryBlockWithAllowedExceptionHandling(
        IOperation invocationOperation,
        INamedTypeSymbol? systemExceptionType,
        ImmutableArray<INamedTypeSymbol> knownJsInteropExceptionTypes)
    {
        var previous = invocationOperation;
        var current = invocationOperation.Parent;

        while (current is not null)
        {
            switch (current)
            {
                case IMethodBodyOperation:
                case IConstructorBodyOperation:
                case IAnonymousFunctionOperation:
                case ILocalFunctionOperation:
                    return false;
                case ITryOperation tryOperation when ReferenceEquals(previous, tryOperation.Body) && HasAllowedExceptionHandling(tryOperation, systemExceptionType, knownJsInteropExceptionTypes):
                    return true;
            }

            previous = current;
            current = current.Parent;
        }

        return false;
    }

    private static bool HasAllowedExceptionHandling(
        ITryOperation tryOperation,
        INamedTypeSymbol? systemExceptionType,
        ImmutableArray<INamedTypeSymbol> knownJsInteropExceptionTypes)
    {
        if (HasCatchAllClause(tryOperation.Catches, systemExceptionType))
        {
            return true;
        }

        if (knownJsInteropExceptionTypes.IsEmpty)
        {
            return false;
        }

        var handledExceptionTypes = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var catchClause in tryOperation.Catches)
        {
            if (catchClause.Filter is not null)
            {
                continue;
            }

            var exceptionType = catchClause.ExceptionType as INamedTypeSymbol;
            if (exceptionType is null)
            {
                continue;
            }

            handledExceptionTypes.Add(exceptionType);
        }

        foreach (var exceptionType in knownJsInteropExceptionTypes)
        {
            if (handledExceptionTypes.Contains(exceptionType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCatchAllClause(ImmutableArray<ICatchClauseOperation> catchClauses, INamedTypeSymbol? systemExceptionType)
    {
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.Filter is not null)
            {
                continue;
            }

            if (catchClause.ExceptionType is null)
            {
                return true;
            }

            if (systemExceptionType is not null &&
                SymbolEqualityComparer.Default.Equals(catchClause.ExceptionType, systemExceptionType))
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

        if (ImplementsInterface(type, ijsRuntimeType))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsInProcessRuntimeType))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsObjectReferenceType))
        {
            return true;
        }

        if (ImplementsInterface(type, ijsInProcessObjectReferenceType))
        {
            return true;
        }

        return false;
    }

    private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceType)
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
