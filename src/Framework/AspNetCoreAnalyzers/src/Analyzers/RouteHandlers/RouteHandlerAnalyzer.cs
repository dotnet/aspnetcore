// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private const int DelegateParameterOrdinal = 2;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters,
        DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers,
        DiagnosticDescriptors.DetectMisplacedLambdaAttribute,
        DiagnosticDescriptors.DetectMismatchedParameterOptionality,
        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
        DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT,
        DiagnosticDescriptors.AmbiguousRouteHandlerRoute,
        DiagnosticDescriptors.AtMostOneFromBodyAttribute,
        DiagnosticDescriptors.InvalidRouteConstraintForParameterType
    ];

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

                // Already checked there are 3 arguments
                var deleateArg = invocation.Arguments[DelegateParameterOrdinal];
                var delegateCreation = (IDelegateCreationOperation?)deleateArg.Descendants().FirstOrDefault(static d => d is IDelegateCreationOperation);

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

                AnalyzeRouteConstraints(routeUsage, wellKnownTypes, context);

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
        var argumentOperation = invocation.Arguments[1];

        if (argumentOperation.Value is not ILiteralOperation literal)
        {
            token = default;
            return false;
        }

        var syntax = (LiteralExpressionSyntax)literal.Syntax;
        token = syntax.Token;
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

    private static void AnalyzeRouteConstraints(RouteUsageModel routeUsage, WellKnownTypes wellKnownTypes, OperationAnalysisContext context)
    {
        foreach (var routeParam in routeUsage.RoutePattern.RouteParameters)
        {
            var handlerParam = GetHandlerParam(routeParam.Name, routeUsage);

            if (handlerParam is null)
            {
                continue;
            }

            foreach (var policy in routeParam.Policies)
            {
                if (IsConstraintInvalidForType(policy, handlerParam.Type, wellKnownTypes))
                {
                    var descriptor = DiagnosticDescriptors.InvalidRouteConstraintForParameterType;
                    var start = routeParam.Span.Start + routeParam.Name.Length + 2; // including '{' and ':'
                    var textSpan = new TextSpan(start, routeParam.Span.End - start - 1); // excluding '}'
                    var location = Location.Create(context.FilterTree, textSpan);
                    var diagnostic = Diagnostic.Create(descriptor, location, policy.AsMemory(1), routeParam.Name, handlerParam.Type.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsConstraintInvalidForType(string policy, ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        if (policy.EndsWith(")", StringComparison.Ordinal)) // Parameterized constraint
        {
            var braceIndex = policy.IndexOf('(');

            if (braceIndex == -1)
            {
                return false;
            }

            var constraint = policy.AsSpan(1, braceIndex - 1);

            return constraint switch
            {
                "length" or "minlength" or "maxlength" or "regex" when type.SpecialType is not SpecialType.System_String => true,
                "min" or "max" or "range" when !IsIntegerType(type) && !IsNullableIntegerType(type) => true,
                _ => false
            };
        }
        else // Simple constraint
        {
            var constraint = policy.AsSpan(1);

            return constraint switch
            {
                "int" when !IsIntegerType(type) && !IsNullableIntegerType(type) => true,
                "bool" when !IsValueTypeOrNullableValueType(type, SpecialType.System_Boolean) => true,
                "datetime" when !IsValueTypeOrNullableValueType(type, SpecialType.System_DateTime) => true,
                "double" when !IsValueTypeOrNullableValueType(type, SpecialType.System_Double) => true,
                "guid" when !IsGuidType(type, wellKnownTypes) && !IsNullableGuidType(type, wellKnownTypes) => true,
                "long" when !IsLongType(type) && !IsNullableLongType(type) => true,
                "decimal" when !IsValueTypeOrNullableValueType(type, SpecialType.System_Decimal) => true,
                "float" when !IsValueTypeOrNullableValueType(type, SpecialType.System_Single) => true,
                "alpha" when type.SpecialType is not SpecialType.System_String => true,
                "file" or "nonfile" when type.SpecialType is not SpecialType.System_String => true,
                _ => false
            };
        }
    }

    private static IParameterSymbol? GetHandlerParam(string name, RouteUsageModel routeUsage)
    {
        foreach (var param in routeUsage.UsageContext.Parameters)
        {
            if (param.Name.Equals(name, StringComparison.Ordinal))
            {
                return (IParameterSymbol)param;
            }
        }

        return null;
    }

    private static bool IsGuidType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        return type.Equals(wellKnownTypes.Get(WellKnownType.System_Guid), SymbolEqualityComparer.Default);
    }

    private static bool IsIntegerType(ITypeSymbol type)
    {
        return type.SpecialType >= SpecialType.System_SByte && type.SpecialType <= SpecialType.System_UInt64;
    }

    private static bool IsLongType(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64;
    }

    private static bool IsNullableGuidType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        return IsNullableType(type, out var namedType) && IsGuidType(namedType.TypeArguments[0], wellKnownTypes);
    }

    private static bool IsNullableIntegerType(ITypeSymbol type)
    {
        return IsNullableType(type, out var namedType) && IsIntegerType(namedType.TypeArguments[0]);
    }

    private static bool IsNullableLongType(ITypeSymbol type)
    {
        return IsNullableType(type, out var namedType) && IsLongType(namedType.TypeArguments[0]);
    }

    public static bool IsNullableType(ITypeSymbol type, [NotNullWhen(true)] out INamedTypeSymbol? namedType)
    {
        namedType = type as INamedTypeSymbol;
        return namedType != null && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
    }

    private static bool IsNullableValueType(ITypeSymbol type, SpecialType specialType)
    {
        return IsNullableType(type, out var namedType) && namedType.TypeArguments[0].SpecialType == specialType;
    }

    private static bool IsValueTypeOrNullableValueType(ITypeSymbol type, SpecialType specialType)
    {
        return type.SpecialType == specialType || IsNullableValueType(type, specialType);
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
