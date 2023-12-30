// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Http;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RequestDelegateReturnTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            context.RegisterOperationAction(context =>
            {
                var methodReference = (IMethodReferenceOperation)context.Operation;
                if (methodReference.Parent is { } parent &&
                    parent.Kind == OperationKind.DelegateCreation &&
                    SymbolEqualityComparer.Default.Equals(parent.Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_RequestDelegate)))
                {
                    // Inspect return type of method signature for Task<T>.
                    var returnType = methodReference.Method.ReturnType;

                    if (SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_Task_T)))
                    {
                        AddDiagnosticWarning(context, methodReference.Syntax.GetLocation(), returnType);
                    }
                }
            }, OperationKind.MethodReference);
            context.RegisterOperationAction(context =>
            {
                var anonymousFunction = (IAnonymousFunctionOperation)context.Operation;
                if (anonymousFunction.Parent is { } parent &&
                    parent.Kind == OperationKind.DelegateCreation &&
                    SymbolEqualityComparer.Default.Equals(parent.Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_RequestDelegate)))
                {
                    // Inspect contents of anonymous function and search for return statements.
                    // Return statement of Task<T> means a value was returned.
                    foreach (var item in anonymousFunction.Body.Descendants())
                    {
                        if (item is IReturnOperation returnOperation &&
                            returnOperation.ReturnedValue is { } returnedValue)
                        {
                            var resolvedOperation = WalkDownConversion(returnedValue);
                            var returnType = resolvedOperation.Type;

                            // Return type could be null if:
                            // 1. The method returns null.
                            // 2. The method throws an exception.
                            if (returnType != null && SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_Task_T)))
                            {
                                AddDiagnosticWarning(context, anonymousFunction.Syntax.GetLocation(), returnType);
                                return;
                            }
                        }
                    }
                }
            }, OperationKind.AnonymousFunction);
        });
    }

    private static void AddDiagnosticWarning(OperationAnalysisContext context, Location location, ITypeSymbol returnType)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate,
            location,
            ((INamedTypeSymbol)returnType).TypeArguments[0].ToString()));
    }

    private static IOperation WalkDownConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversionOperation)
        {
            operation = conversionOperation.Operand;
        }

        return operation;
    }
}
