// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ActionResultOfTReturnTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.MVC1007_ActionResultOfTTypeMismatch);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!SymbolCache.TryCreate(compilationContext.Compilation, out var symbolCache))
            {
                return;
            }

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeReturnOperation(operationContext, symbolCache),
                OperationKind.Return);
        });
    }

    private static void AnalyzeReturnOperation(OperationAnalysisContext context, SymbolCache symbolCache)
    {
        var returnOperation = (IReturnOperation)context.Operation;
        if (returnOperation.ReturnedValue is null)
        {
            return;
        }

        var containingMethod = GetContainingMethod(returnOperation);
        if (containingMethod is null)
        {
            return;
        }

        var declaredTypeArgument = GetActionResultTypeArgument(containingMethod, symbolCache);
        if (declaredTypeArgument is null)
        {
            return;
        }

        var returnedValue = returnOperation.ReturnedValue;

        // Unwrap implicit conversions (e.g. ActionResult<T> implicit operator)
        while (returnedValue is IConversionOperation conversion && conversion.IsImplicit)
        {
            returnedValue = conversion.Operand;
        }

        var returnedType = returnedValue.Type;
        if (returnedType is null)
        {
            return;
        }

        // Check if the returned value is a 2xx result type with an object value argument
        if (!TryGetObjectValueType(returnedValue, symbolCache, out var objectValueType))
        {
            return;
        }

        if (objectValueType is null)
        {
            // null literal — valid for any reference type
            return;
        }

        if (!IsAssignableTo(objectValueType, declaredTypeArgument, context.Compilation))
        {
            // Unwrap the returned value back to the invocation to get the right location
            var returnedValueForLocation = returnOperation.ReturnedValue;
            while (returnedValueForLocation is IConversionOperation conv && conv.IsImplicit)
            {
                returnedValueForLocation = conv.Operand;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MVC1007_ActionResultOfTTypeMismatch,
                returnedValueForLocation.Syntax.GetLocation(),
                objectValueType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                declaredTypeArgument.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }

    private static IMethodSymbol? GetContainingMethod(IOperation operation)
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return null;
            }
        }

        return operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
    }

    internal static ITypeSymbol? GetActionResultTypeArgument(IMethodSymbol method, SymbolCache symbolCache)
    {
        var returnType = method.ReturnType;

        // Handle Task<ActionResult<T>> and ValueTask<ActionResult<T>>
        if (returnType is INamedTypeSymbol namedReturn && namedReturn.IsGenericType)
        {
            var definition = namedReturn.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(definition, symbolCache.TaskOfT) ||
                SymbolEqualityComparer.Default.Equals(definition, symbolCache.ValueTaskOfT))
            {
                returnType = namedReturn.TypeArguments[0];
            }
        }

        if (returnType is INamedTypeSymbol actionResultType &&
            actionResultType.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(actionResultType.OriginalDefinition, symbolCache.ActionResultOfT))
        {
            return actionResultType.TypeArguments[0];
        }

        return null;
    }

    private static bool TryGetObjectValueType(
        IOperation returnedValue,
        SymbolCache symbolCache,
        out ITypeSymbol? objectValueType)
    {
        objectValueType = null;

        if (returnedValue is not IInvocationOperation invocation)
        {
            return false;
        }

        var method = invocation.TargetMethod;
        var returnType = method.ReturnType;

        // Check if the return type has a [DefaultStatusCode] that is 2xx
        if (!Is2xxResultType(returnType, symbolCache))
        {
            return false;
        }

        // Find the parameter annotated with [ActionResultObjectValue]
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];
            if (HasActionResultObjectValueAttribute(parameter))
            {
                var argument = GetArgumentForParameter(invocation, parameter);
                if (argument is null)
                {
                    return false;
                }

                var argumentValue = argument.Value;
                // Unwrap implicit conversions (e.g. boxing int to object)
                while (argumentValue is IConversionOperation conv && conv.IsImplicit)
                {
                    argumentValue = conv.Operand;
                }

                objectValueType = argumentValue.Type;
                return true;
            }
        }

        return false;
    }

    private static IArgumentOperation? GetArgumentForParameter(IInvocationOperation invocation, IParameterSymbol parameter)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (SymbolEqualityComparer.Default.Equals(argument.Parameter, parameter))
            {
                if (argument.ArgumentKind == ArgumentKind.DefaultValue)
                {
                    return null;
                }
                return argument;
            }
        }

        return null;
    }

    private static bool Is2xxResultType(ITypeSymbol type, SymbolCache symbolCache)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbolCache.DefaultStatusCodeAttribute))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is int statusCode &&
                statusCode >= 200 && statusCode < 300)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasActionResultObjectValueAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "ActionResultObjectValueAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAssignableTo(ITypeSymbol source, ITypeSymbol destination, Compilation compilation)
    {
        if (compilation is not CSharpCompilation csharpCompilation)
        {
            return true;
        }

        var conversion = csharpCompilation.ClassifyConversion(source, destination);
        return conversion.IsIdentity || conversion.IsImplicit;
    }

    internal readonly struct SymbolCache
    {
        public SymbolCache(
            INamedTypeSymbol actionResultOfT,
            INamedTypeSymbol defaultStatusCodeAttribute,
            INamedTypeSymbol taskOfT,
            INamedTypeSymbol valueTaskOfT)
        {
            ActionResultOfT = actionResultOfT;
            DefaultStatusCodeAttribute = defaultStatusCodeAttribute;
            TaskOfT = taskOfT;
            ValueTaskOfT = valueTaskOfT;
        }

        public static bool TryCreate(Compilation compilation, out SymbolCache symbolCache)
        {
            symbolCache = default;

            var actionResultOfT = compilation.GetTypeByMetadataName(SymbolNames.ActionResultOfT);
            if (actionResultOfT is null)
            {
                return false;
            }

            var defaultStatusCodeAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute");
            if (defaultStatusCodeAttribute is null)
            {
                return false;
            }

            var taskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (taskOfT is null)
            {
                return false;
            }

            var valueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            if (valueTaskOfT is null)
            {
                return false;
            }

            symbolCache = new SymbolCache(actionResultOfT, defaultStatusCodeAttribute, taskOfT, valueTaskOfT);
            return true;
        }

        public INamedTypeSymbol ActionResultOfT { get; }
        public INamedTypeSymbol DefaultStatusCodeAttribute { get; }
        public INamedTypeSymbol TaskOfT { get; }
        public INamedTypeSymbol ValueTaskOfT { get; }
    }
}
