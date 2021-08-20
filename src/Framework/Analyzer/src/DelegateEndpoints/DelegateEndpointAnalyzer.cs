// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class DelegateEndpointAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.DoNotUseModelBindingAttributesOnDelegateEndpointParameters,
    });

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartAnalysisContext =>
        {
            var compilation = compilationStartAnalysisContext.Compilation;
            if (!WellKnownTypes.TryCreate(compilation, out var wellKnownTypes))
            {
                return;
            }

            compilationStartAnalysisContext.RegisterOperationAction(operationAnalysisContext =>
            {
                var invocation = (IInvocationOperation)operationAnalysisContext.Operation;
                var targetMethod = invocation.TargetMethod;
                if (IsDelegateHandlerInvocation(wellKnownTypes, invocation, targetMethod))
                {
                    return;
                }

                var delegateCreation = invocation.Arguments[2].Descendants().OfType<IDelegateCreationOperation>().FirstOrDefault();
                if (delegateCreation is null)
                {
                    return;
                }

                if (delegateCreation.Target.Kind == OperationKind.AnonymousFunction)
                {
                    var lambda = ((IAnonymousFunctionOperation)delegateCreation.Target);
                    DisallowMvcBindArgumentsOnParameters(in operationAnalysisContext, wellKnownTypes, invocation, lambda.Symbol);
                }
                else if (delegateCreation.Target.Kind == OperationKind.MethodReference)
                {
                    var methodReference = (IMethodReferenceOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in operationAnalysisContext, wellKnownTypes, invocation, methodReference.Method);
                }
            }, OperationKind.Invocation);
        });
    }

    private static bool IsDelegateHandlerInvocation(
        WellKnownTypes wellKnownTypes,
        IInvocationOperation invocation,
        IMethodSymbol targetMethod)
    {
        return !targetMethod.Name.StartsWith("Map", StringComparison.Ordinal) ||
            !SymbolEqualityComparer.Default.Equals(wellKnownTypes.DelegateEndpointRouteBuilderExtensions, targetMethod.ContainingType) ||
            invocation.Arguments.Length != 3;
    }
}
