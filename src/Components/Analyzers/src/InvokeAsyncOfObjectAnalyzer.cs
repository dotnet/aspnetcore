// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that detects usage of InvokeAsync&lt;object&gt; and recommends using InvokeVoidAsync instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvokeAsyncOfObjectAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var ijsRuntimeType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.JSInterop.IJSRuntime");
            var ijsObjectReferenceType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.JSInterop.IJSObjectReference");
            var ijsInProcessRuntimeType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.JSInterop.IJSInProcessRuntime");
            var ijsInProcessObjectReferenceType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.JSInterop.IJSInProcessObjectReference");
            var objectType = compilationContext.Compilation.GetSpecialType(SpecialType.System_Object);

            if (ijsRuntimeType is null && ijsObjectReferenceType is null)
            {
                // JSInterop types are not available
                return;
            }

            compilationContext.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                var targetMethod = invocation.TargetMethod;

                // Check if the method is named InvokeAsync and is generic
                if (targetMethod.Name != "InvokeAsync" || !targetMethod.IsGenericMethod)
                {
                    return;
                }

                // Check if the type argument is object
                if (targetMethod.TypeArguments.Length != 1 ||
                    !SymbolEqualityComparer.Default.Equals(targetMethod.TypeArguments[0], objectType))
                {
                    return;
                }

                // Check if the method is on IJSRuntime, IJSObjectReference, or their in-process variants
                // This includes extension methods on these types
                var containingType = targetMethod.ContainingType;
                var receiverType = GetReceiverType(invocation);

                if (!IsJSInteropType(receiverType, ijsRuntimeType, ijsObjectReferenceType, ijsInProcessRuntimeType, ijsInProcessObjectReferenceType) &&
                    !IsJSInteropExtensionClass(containingType))
                {
                    return;
                }

                operationContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn,
                    invocation.Syntax.GetLocation()));
            }, OperationKind.Invocation);
        });
    }

    private static ITypeSymbol? GetReceiverType(IInvocationOperation invocation)
    {
        // For extension methods, the first argument is the receiver
        if (invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
        {
            return invocation.Arguments[0].Value.Type;
        }

        // For instance methods
        return invocation.Instance?.Type;
    }

    private static bool IsJSInteropType(
        ITypeSymbol? type,
        INamedTypeSymbol? ijsRuntimeType,
        INamedTypeSymbol? ijsObjectReferenceType,
        INamedTypeSymbol? ijsInProcessRuntimeType,
        INamedTypeSymbol? ijsInProcessObjectReferenceType)
    {
        if (type is null)
        {
            return false;
        }

        // Check if the type implements any of the JSInterop interfaces
        if (ijsRuntimeType is not null && ImplementsInterface(type, ijsRuntimeType))
        {
            return true;
        }

        if (ijsObjectReferenceType is not null && ImplementsInterface(type, ijsObjectReferenceType))
        {
            return true;
        }

        if (ijsInProcessRuntimeType is not null && ImplementsInterface(type, ijsInProcessRuntimeType))
        {
            return true;
        }

        if (ijsInProcessObjectReferenceType is not null && ImplementsInterface(type, ijsInProcessObjectReferenceType))
        {
            return true;
        }

        return false;
    }

    private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
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

    private static bool IsJSInteropExtensionClass(INamedTypeSymbol containingType)
    {
        // Check for the extension classes that provide InvokeAsync methods
        var fullName = containingType.ToDisplayString();
        return fullName == "Microsoft.JSInterop.JSRuntimeExtensions" ||
               fullName == "Microsoft.JSInterop.JSObjectReferenceExtensions" ||
               fullName == "Microsoft.JSInterop.JSInProcessRuntimeExtensions" ||
               fullName == "Microsoft.JSInterop.JSInProcessObjectReferenceExtensions";
    }
}
