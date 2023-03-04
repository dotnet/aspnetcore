// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private const int DelegateParameterOrdinal = 2;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters,
        DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers,
        DiagnosticDescriptors.DetectMisplacedLambdaAttribute,
        DiagnosticDescriptors.DetectMismatchedParameterOptionality,
        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
        DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT,
        DiagnosticDescriptors.AmbiguousRouteHandlerRoute,
        DiagnosticDescriptors.AtMostOneFromBodyAttribute
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
            var routeUsageCache = RouteUsageCache.GetOrCreate(compilation);

            // We want ConcurrentHashSet here in case RegisterOperationAction runs in parallel.
            // Since ConcurrentHashSet doesn't exist, use ConcurrentDictionary and ignore the value.
            var concurrentQueue = new ConcurrentQueue<ConcurrentDictionary<MapOperation, byte>>();
            context.RegisterOperationBlockStartAction(context =>
            {
                // Pool and reuse lists for each block.
                if (!concurrentQueue.TryDequeue(out var mapOperations))
                {
                    mapOperations = new ConcurrentDictionary<MapOperation, byte>();
                }

                context.RegisterOperationAction(c => DoOperationAnalysis(c, mapOperations), OperationKind.Invocation);

                context.RegisterOperationBlockEndAction(c =>
                {
                    DetectAmbiguousRoutes(c, wellKnownTypes, mapOperations);

                    // Return to the pool.
                    mapOperations.Clear();
                    concurrentQueue.Enqueue(mapOperations);
                });
            });

            void DoOperationAnalysis(OperationAnalysisContext context, ConcurrentDictionary<MapOperation, byte> mapOperations)
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

                if (!TryGetStringToken(invocation, out var token))
                {
                    return;
                }

                var routeUsage = routeUsageCache.Get(token, context.CancellationToken);
                if (routeUsage is null)
                {
                    return;
                }

                mapOperations.TryAdd(MapOperation.Create(invocation, routeUsage), value: default);

                if (delegateCreation.Target.Kind == OperationKind.AnonymousFunction)
                {
                    var lambda = (IAnonymousFunctionOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, lambda.Symbol);
                    DisallowNonParsableComplexTypesOnParameters(in context, wellKnownTypes, routeUsage, lambda.Symbol);
                    DisallowReturningActionResultFromMapMethods(in context, wellKnownTypes, invocation, lambda, delegateCreation.Syntax);
                    DetectMisplacedLambdaAttribute(context, lambda);
                    DetectMismatchedParameterOptionality(in context, routeUsage, lambda.Symbol);
                    AtMostOneFromBodyAttribute(in context, wellKnownTypes, lambda.Symbol);
                }
                else if (delegateCreation.Target.Kind == OperationKind.MethodReference)
                {
                    var methodReference = (IMethodReferenceOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, methodReference.Method);
                    DisallowNonParsableComplexTypesOnParameters(in context, wellKnownTypes, routeUsage, methodReference.Method);
                    DetectMismatchedParameterOptionality(in context, routeUsage, methodReference.Method);
                    AtMostOneFromBodyAttribute(in context, wellKnownTypes, methodReference.Method);

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
            }
        });
    }

    private static bool TryGetStringToken(IInvocationOperation invocation, out SyntaxToken token)
    {
        IArgumentOperation? argumentOperation = null;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == 1)
            {
                argumentOperation = argument;
            }
        }

        if (argumentOperation?.Syntax is not ArgumentSyntax routePatternArgumentSyntax ||
            routePatternArgumentSyntax.Expression is not LiteralExpressionSyntax routePatternArgumentLiteralSyntax)
        {
            token = default;
            return false;
        }

        token = routePatternArgumentLiteralSyntax.Token;
        return true;
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
            IsCompatibleDelegateType(wellKnownTypes, targetMethod);

        static bool IsCompatibleDelegateType(WellKnownTypes wellKnownTypes, IMethodSymbol targetMethod)
        {
            var parmeterType = targetMethod.Parameters[DelegateParameterOrdinal].Type;
            if (SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.System_Delegate), parmeterType))
            {
                return true;
            }
            if (SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_RequestDelegate), parmeterType))
            {
                return true;
            }
            return false;
        }
    }

    private record struct MapOperation(IOperation? Builder, IInvocationOperation Operation, RouteUsageModel RouteUsageModel)
    {
        public static MapOperation Create(IInvocationOperation operation, RouteUsageModel routeUsageModel)
        {
            IOperation? builder = null;

            var builderArgument = operation.Arguments.SingleOrDefault(a => a.Parameter?.Ordinal == 0);
            if (builderArgument != null)
            {
                builder = WalkDownConversion(builderArgument.Value);
            }

            return new MapOperation(builder, operation, routeUsageModel);
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
}
