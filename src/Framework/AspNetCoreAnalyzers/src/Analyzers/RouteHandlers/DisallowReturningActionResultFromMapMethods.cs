// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowReturningActionResultFromMapMethods(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IInvocationOperation invocationOperation,
        IAnonymousFunctionOperation anonymousFunction,
        SyntaxNode nodeForError)
    {
        DisallowReturningActionResultFromMapMethods(in context, wellKnownTypes, invocationOperation, anonymousFunction.Symbol, anonymousFunction.Body, nodeForError);
    }

    private static void DisallowReturningActionResultFromMapMethods(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IInvocationOperation invocationOperation,
        IMethodSymbol methodSymbol,
        IBlockOperation? methodBody,
        SyntaxNode nodeForError)
    {
        var returnType = UnwrapPossibleAsyncReturnType(methodSymbol.ReturnType);

        if (wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IResult).IsAssignableFrom(returnType))
        {
            // This type returns some form of IResult. Nothing to do here.
            return;
        }

        if (methodBody is null &&
            (wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_IActionResult).IsAssignableFrom(returnType) ||
            wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_Infrastructure_IConvertToActionResult).IsAssignableFrom(returnType)))
        {
            // if we don't have a method body, and the action is IResult or ActionResult<T> returning, produce diagnostics for the entire method.
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers,
                nodeForError.GetLocation(),
                invocationOperation.TargetMethod.Name));
            return;
        }

        foreach (var returnOperation in methodBody.Descendants().OfType<IReturnOperation>())
        {
            if (returnOperation.ReturnedValue is null or IInvalidOperation)
            {
                continue;
            }

            var returnedValue = returnOperation.ReturnedValue;
            if (returnedValue is IConversionOperation conversionOperation)
            {
                returnedValue = conversionOperation.Operand;
            }

            var type = returnedValue.Type;

            if (type is null)
            {
                continue;
            }

            if (wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IResult).IsAssignableFrom(type))
            {
                continue;
            }

            if (wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_IActionResult).IsAssignableFrom(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers,
                    returnOperation.Syntax.GetLocation(),
                    invocationOperation.TargetMethod.Name));
            }
        }
    }

    private static ITypeSymbol UnwrapPossibleAsyncReturnType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol { Name: "Task" or "ValueTask", IsGenericType: true, TypeArguments: { Length: 1 } } taskLike)
        {
            return returnType;
        }

        return taskLike.TypeArguments[0];
    }
}
