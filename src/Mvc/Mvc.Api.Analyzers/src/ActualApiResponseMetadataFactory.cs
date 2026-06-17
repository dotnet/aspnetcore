// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public static class ActualApiResponseMetadataFactory
{
    /// <summary>
    /// This method looks at individual return statments and attempts to parse the status code and the return type.
    /// Given an <see cref="IMethodBodyBaseOperation"/> for an action, this method inspects return statements in the body.
    /// If the returned type is not assignable from IActionResult, it assumes that an "object" value is being returned. e.g. return new Person();
    /// For return statements returning an action result, it attempts to infer the status code and return type. Helper methods in controller,
    /// values set in initializer and new-ing up an IActionResult instance are supported.
    /// </summary>
    internal static bool TryGetActualResponseMetadata(
        in ApiControllerSymbolCache symbolCache,
        IMethodBodyBaseOperation methodBody,
        out IList<ActualApiResponseMetadata> actualResponseMetadata)
    {
        var localActualResponseMetadata = new List<ActualApiResponseMetadata>();

        var localSymbolCache = symbolCache;
        var allReturnStatementsReadable = true;

        void AnalyzeResponseExpression(IReturnOperation returnOperation)
        {
            var responseMetadata = InspectReturnOperation(
                localSymbolCache,
                returnOperation);

            foreach (var metadata in responseMetadata)
            {
                if (!metadata.HasValue)
                {
                    allReturnStatementsReadable = false;
                    continue;
                }

                localActualResponseMetadata.Add(metadata.Value);
            }
        }

        foreach (var operation in GetReturnStatements(methodBody))
        {
            AnalyzeResponseExpression(operation);
        }

        actualResponseMetadata = localActualResponseMetadata;
        return allReturnStatementsReadable;
    }

    internal static ActualApiResponseMetadata?[] InspectReturnOperation(
        in ApiControllerSymbolCache symbolCache,
        IReturnOperation returnOperation,
        ISwitchExpressionArmOperation? armOperation = null)
    {
        var returnedValue = armOperation?.Value ?? returnOperation.ReturnedValue;
        var defaultStatusCodeAttributeSymbol = symbolCache.DefaultStatusCodeAttribute;

        if (returnedValue is null || returnedValue is IInvalidOperation)
        {
            return [null];
        }

        // Covers conversion in the `IActionResult GetResult => NotFound()` case.
        // Multiple conversions can happen for ActionResult<T>, hence a while loop.
        while (returnedValue is IConversionOperation conversion)
        {
            returnedValue = conversion.Operand;
        }

        var statementReturnType = returnedValue.Type;

        if (statementReturnType is not null && !symbolCache.IActionResult.IsAssignableFrom(statementReturnType))
        {
            // Return expression is not an instance of IActionResult. Must be returning the "model".
            return [new ActualApiResponseMetadata(returnOperation, statementReturnType)];
        }

        var defaultStatusCodeAttribute = statementReturnType
            ?.GetAttributes(defaultStatusCodeAttributeSymbol, inherit: true)
            ?.FirstOrDefault();

        // If the type is not annotated with a default status code, then examine
        // the attributes on any invoked method returning the type.
        if (defaultStatusCodeAttribute is null && returnedValue is IInvocationOperation invocationOperation)
        {
            defaultStatusCodeAttribute = invocationOperation.TargetMethod
                .GetAttributes(defaultStatusCodeAttributeSymbol)
                .FirstOrDefault();
        }

        var statusCode = GetDefaultStatusCode(defaultStatusCodeAttribute);

        if (returnedValue is ISwitchExpressionOperation switchExpression)
        {
            var metadata = new List<ActualApiResponseMetadata?>();

            for (var i = 0; i < switchExpression.Arms.Length; i++)
            {
                var arm = switchExpression.Arms[i];
                var armMetadata = InspectReturnOperation(symbolCache, returnOperation, arm);
                metadata.AddRange(armMetadata);
            }

            return metadata.ToArray();
        }

        ITypeSymbol? returnType = null;
        switch (returnedValue)
        {
            case IInvocationOperation invocation:
                {
                    // Covers the 'return StatusCode(200)' case.
                    var result = InspectMethodArguments(invocation.Arguments);
                    statusCode = result.statusCode ?? statusCode;
                    returnType = result.returnType;
                    break;
                }

            case IObjectCreationOperation creation:
                {
                    // Read values from 'return new StatusCodeResult(200) case.
                    var result = InspectMethodArguments(creation.Arguments);
                    statusCode = result.statusCode ?? statusCode;
                    returnType = result.returnType;

                    // Read values from property assignments e.g. 'return new ObjectResult(...) { StatusCode = 200 }'.
                    // Property assignments override constructor assigned values and defaults.
                    if (creation.Initializer is not null)
                    {
                        result = InspectInitializers(symbolCache, creation.Initializer);
                        statusCode = result.statusCode ?? statusCode;
                        returnType = result.returnType ?? returnType;
                    }
                    break;
                }
        }

        if (statusCode == null)
        {
            return [null];
        }

        return [new ActualApiResponseMetadata(returnOperation, statusCode.Value, returnType)];
    }

    private static (int? statusCode, ITypeSymbol? returnType) InspectInitializers(
        in ApiControllerSymbolCache symbolCache,
        IObjectOrCollectionInitializerOperation initializer)
    {
        int? statusCode = null;
        ITypeSymbol? typeSymbol = null;

        foreach (var child in initializer.Initializers)
        {
            if (child is not IAssignmentOperation assignmentOperation ||
                assignmentOperation.Target is not IPropertyReferenceOperation propertyReference)
            {
                continue;
            }

            var property = propertyReference.Property;

            if (IsInterfaceImplementation(property, symbolCache.StatusCodeActionResultStatusProperty))
            {
                // Look for assignments to IStatusCodeActionResult.StatusCode
                if (TryGetStatusCode(assignmentOperation.Value, out var statusCodeValue))
                {
                    // new StatusCodeResult { StatusCode = someLocal };
                    statusCode = statusCodeValue;
                }
            }
            else if (HasAttributeNamed(property, ApiSymbolNames.ActionResultObjectValueAttribute))
            {
                // Look for assignment to a property annotated with [ActionResultObjectValue]
                typeSymbol = assignmentOperation.Type;
            }
        }

        return (statusCode, typeSymbol);
    }

    private static (int? statusCode, ITypeSymbol? returnType) InspectMethodArguments(ImmutableArray<IArgumentOperation> arguments)
    {
        int? statusCode = null;
        ITypeSymbol? typeSymbol = null;

        foreach (var argument in arguments)
        {
            var parameter = argument.Parameter;
            if (HasAttributeNamed(parameter, ApiSymbolNames.ActionResultStatusCodeAttribute))
            {
                if (TryGetStatusCode(argument.Value, out var statusCodeValue))
                {
                    statusCode = statusCodeValue;
                }
            }

            if (HasAttributeNamed(parameter, ApiSymbolNames.ActionResultObjectValueAttribute))
            {
                var operation = argument.Value;

                while (operation is IConversionOperation conversionOperation)
                {
                    // new BadRequest((object)MyDataType);
                    operation = conversionOperation.Operand;
                }

                typeSymbol = operation.Type;
            }
        }

        return (statusCode, typeSymbol);
    }

    private static bool TryGetStatusCode(
        IOperation operation,
        out int statusCode)
    {
        while (operation is IConversionOperation conversion)
        {
            // For cases where one can write 'return StatusCode((int)(object)Constant)'
            operation = conversion.Operand;
        }

        if (operation.ConstantValue is { HasValue: true } constant)
        {
            // Covers the 'return StatusCode(Constant)' case.
            // Constant can be literal, field reference, or local reference.
            statusCode = (int)constant.Value;
            return true;
        }

        statusCode = 0;
        return false;
    }

    internal static int? GetDefaultStatusCode(AttributeData? attribute)
    {
        if (attribute != null &&
            attribute.ConstructorArguments.Length == 1 &&
            attribute.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
            attribute.ConstructorArguments[0].Value is int statusCode)
        {
            return statusCode;
        }

        return null;
    }

    private static bool IsInterfaceImplementation(IPropertySymbol property, IPropertySymbol statusCodeActionResultStatusProperty)
    {
        if (property.Name != statusCodeActionResultStatusProperty.Name)
        {
            return false;
        }

        for (var i = 0; i < property.ExplicitInterfaceImplementations.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(property.ExplicitInterfaceImplementations[i], statusCodeActionResultStatusProperty))
            {
                return true;
            }
        }

        var implementedProperty = property.ContainingType.FindImplementationForInterfaceMember(statusCodeActionResultStatusProperty);
        return SymbolEqualityComparer.Default.Equals(implementedProperty, property);
    }

    private static bool HasAttributeNamed(ISymbol symbol, string attributeName)
    {
        var attributes = symbol.GetAttributes();
        var length = attributes.Length;
        for (var i = 0; i < length; i++)
        {
            if (attributes[i].AttributeClass.Name == attributeName)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<IReturnOperation> GetReturnStatements(IMethodBodyBaseOperation method)
    {
        foreach (var returnOperation in method.Descendants().OfType<IReturnOperation>())
        {
            if (!AncestorIsLocalFunction(returnOperation))
            {
                yield return returnOperation;
            }
        }

        bool AncestorIsLocalFunction(IReturnOperation operation)
        {
            var parent = operation.Parent;
            while (parent != method)
            {
                if (parent is ILocalFunctionOperation or IAnonymousFunctionOperation)
                {
                    return true;
                }

                parent = parent.Parent;
            }

            return false;
        }
    }
}
