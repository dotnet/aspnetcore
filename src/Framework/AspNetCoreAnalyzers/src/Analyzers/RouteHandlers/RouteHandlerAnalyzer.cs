// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private const int DelegateParameterOrdinal = 2;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters,
        DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers,
        DiagnosticDescriptors.DetectMisplacedLambdaAttribute,
        DiagnosticDescriptors.DetectMismatchedParameterOptionality,
        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsableOrBindable,
        DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;
                var targetMethod = invocation.TargetMethod;
                if (!IsRouteHandlerInvocation(wellKnownTypes, invocation, targetMethod))
                {
                    return;
                }

                IDelegateCreationOperation? delegateCreation = null;
                foreach (var argument in invocation.Arguments)
                {
                    if (argument.Parameter?.Ordinal == DelegateParameterOrdinal)
                    {
                        delegateCreation = argument.Descendants().OfType<IDelegateCreationOperation>().FirstOrDefault();
                        break;
                    }
                }

                if (delegateCreation is null)
                {
                    return;
                }

                if (delegateCreation.Target.Kind == OperationKind.AnonymousFunction)
                {
                    var lambda = (IAnonymousFunctionOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, lambda.Symbol);
                    DisallowNonParsableComplexTypesOnParameters(in context, invocation, lambda.Symbol);
                    DisallowReturningActionResultFromMapMethods(in context, wellKnownTypes, invocation, lambda, delegateCreation.Syntax);
                    DetectMisplacedLambdaAttribute(context, lambda);
                    DetectMismatchedParameterOptionality(in context, invocation, lambda.Symbol);
                }
                else if (delegateCreation.Target.Kind == OperationKind.MethodReference)
                {
                    var methodReference = (IMethodReferenceOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, methodReference.Method);
                    DisallowNonParsableComplexTypesOnParameters(in context, invocation, methodReference.Method);
                    DetectMismatchedParameterOptionality(in context, invocation, methodReference.Method);

                    var foundMethodReferenceBody = false;
                    if (!methodReference.Method.DeclaringSyntaxReferences.IsEmpty)
                    {
                        var syntaxReference = methodReference.Method.DeclaringSyntaxReferences.Single();
                        var syntaxNode = syntaxReference.GetSyntax(context.CancellationToken);
                        var methodOperation = syntaxNode.SyntaxTree == invocation.SemanticModel!.SyntaxTree
                            ? invocation.SemanticModel.GetOperation(syntaxNode, context.CancellationToken)
                            : null;
                        if (methodOperation is ILocalFunctionOperation { Body: not null } localFunction)
                        {
                            foundMethodReferenceBody = true;
                            DisallowReturningActionResultFromMapMethods(
                                in context,
                                wellKnownTypes,
                                invocation,
                                methodReference.Method,
                                localFunction.Body,
                                delegateCreation.Syntax);
                        }
                        else if (methodOperation is IMethodBodyOperation methodBody)
                        {
                            foundMethodReferenceBody = true;
                            DisallowReturningActionResultFromMapMethods(
                                in context,
                                wellKnownTypes,
                                invocation,
                                methodReference.Method,
                                methodBody.BlockBody ?? methodBody.ExpressionBody,
                                delegateCreation.Syntax);
                        }
                    }

                    if (!foundMethodReferenceBody)
                    {
                        // it's possible we couldn't find the operation for the method reference. In this case,
                        // try and provide less detailed diagnostics to the extent we can
                        DisallowReturningActionResultFromMapMethods(
                                in context,
                                wellKnownTypes,
                                invocation,
                                methodReference.Method,
                                methodBody: null,
                                delegateCreation.Syntax);

                    }
                }
            }, OperationKind.Invocation);
        });
    }

    private static bool IsRouteHandlerInvocation(
        WellKnownTypes wellKnownTypes,
        IInvocationOperation invocation,
        IMethodSymbol targetMethod)
    {
        return targetMethod.Name.StartsWith("Map", StringComparison.Ordinal) &&
            SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_EndpointRouteBuilderExtensions), targetMethod.ContainingType) &&
            invocation.Arguments.Length == 3 &&
            targetMethod.Parameters.Length == 3 &&
            SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.System_Delegate), targetMethod.Parameters[DelegateParameterOrdinal].Type);
    }
}
