// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT,
        DiagnosticDescriptors.AmbiguousRouteHandlerRoute
    );

    private record struct MapOperation(IInvocationOperation Operation, RouteUsageModel RouteUsageModel);

    private readonly struct MapOperationGroupKey : IEquatable<MapOperationGroupKey>
    {
        public IOperation? ParentOperation { get; }
        public RoutePatternTree RoutePattern { get; }
        public ImmutableArray<string> HttpMethods { get; }

        public MapOperationGroupKey(IOperation operation, RoutePatternTree routePattern, ImmutableArray<string> httpMethods)
        {
            Debug.Assert(!httpMethods.IsDefault);

            ParentOperation = GetParentOperation(operation);
            RoutePattern = routePattern;
            HttpMethods = httpMethods;
        }

        private static IOperation? GetParentOperation(IOperation operation)
        {
            var parent = operation.Parent;
            while (parent is not null)
            {
                if (parent is IBlockOperation)
                {
                    return parent;
                }

                parent = parent.Parent;
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is MapOperationGroupKey key)
            {
                return Equals(key);
            }
            return false;
        }

        public bool Equals(MapOperationGroupKey other)
        {
            return
                Equals(ParentOperation, other.ParentOperation) &&
                RoutePatternComparer.Instance.Equals(RoutePattern, other.RoutePattern) &&
                HasMatchingHttpMethods(HttpMethods, other.HttpMethods);
        }

        private static bool HasMatchingHttpMethods(ImmutableArray<string> httpMethods1, ImmutableArray<string> httpMethods2)
        {
            if (httpMethods1.IsEmpty || httpMethods2.IsEmpty)
            {
                return true;
            }

            foreach (var item1 in httpMethods1)
            {
                foreach (var item2 in httpMethods2)
                {
                    if (item2 == item1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (ParentOperation?.GetHashCode() ?? 0) ^ RoutePatternComparer.Instance.GetHashCode(RoutePattern);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
            var routeUsageCache = RouteUsageCache.GetOrCreate(compilation);
            var blockRouteUsage = new List<MapOperation>();

            context.RegisterOperationBlockStartAction(context =>
            {
                context.RegisterOperationAction(DoOperationAnalysis, OperationKind.Invocation);
                blockRouteUsage.Clear();
            });
            context.RegisterOperationBlockAction(context =>
            {
                var groupedByParent = blockRouteUsage
                    .Where(u => !u.RouteUsageModel.UsageContext.HttpMethods.IsDefault)
                    .GroupBy(u => new MapOperationGroupKey(u.Operation, u.RouteUsageModel.RoutePattern, u.RouteUsageModel.UsageContext.HttpMethods));

                foreach (var ambigiousGroup in groupedByParent.Where(g => g.Count() >= 2))
                {
                    foreach (var ambigiousMapOperation in ambigiousGroup)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.AmbiguousRouteHandlerRoute,
                            ambigiousMapOperation.RouteUsageModel.UsageContext.RouteToken.GetLocation(),
                            ambigiousMapOperation.RouteUsageModel.RoutePattern.Root.ToString()));
                    }
                }
            });

            void DoOperationAnalysis(OperationAnalysisContext context)
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

                blockRouteUsage.Add(new MapOperation(invocation, routeUsage));

                if (delegateCreation.Target.Kind == OperationKind.AnonymousFunction)
                {
                    var lambda = (IAnonymousFunctionOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, lambda.Symbol);
                    DisallowNonParsableComplexTypesOnParameters(in context, routeUsage, lambda.Symbol);
                    DisallowReturningActionResultFromMapMethods(in context, wellKnownTypes, invocation, lambda, delegateCreation.Syntax);
                    DetectMisplacedLambdaAttribute(context, lambda);
                    DetectMismatchedParameterOptionality(in context, routeUsage, lambda.Symbol);
                }
                else if (delegateCreation.Target.Kind == OperationKind.MethodReference)
                {
                    var methodReference = (IMethodReferenceOperation)delegateCreation.Target;
                    DisallowMvcBindArgumentsOnParameters(in context, wellKnownTypes, invocation, methodReference.Method);
                    DisallowNonParsableComplexTypesOnParameters(in context, routeUsage, methodReference.Method);
                    DetectMismatchedParameterOptionality(in context, routeUsage, methodReference.Method);

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
            SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.System_Delegate), targetMethod.Parameters[DelegateParameterOrdinal].Type);
    }
}
