// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectInvalidCustomBidingFormat(
        OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IMethodSymbol methodSymbol)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (!parameter.Type.IsReferenceType)
            {
                continue;
            }

            DetectInvalidMethodFormatFromParameter(
                context: context,
                parameterSymbol: parameter,
                methodName: "BindAsync",
                returnType: wellKnownTypes.ValueTaskOfT,
                parametersValidator: (OperationAnalysisContext context, IMethodSymbol method) => ValidateBindAsyncParameters(method, wellKnownTypes),
                modifiersValidator: ValidateCustomBindingModifiers,
                onInvalidMessage: DiagnosticDescriptors.CustomBindingBindAsyncMustHaveAValidFormat);

            DetectInvalidMethodFormatFromParameter(
                context: context,
                parameterSymbol: parameter,
                methodName: "TryParse",
                returnType: context.Compilation.GetSpecialType(SpecialType.System_Boolean),
                parametersValidator: (OperationAnalysisContext context, IMethodSymbol method) => ValidateTryParseParameters(method, wellKnownTypes),
                modifiersValidator: ValidateCustomBindingModifiers,
                onInvalidMessage: DiagnosticDescriptors.CustomBindingTryParseMustHaveAValidFormat);
        }
    }

    private static bool ValidateTryParseParameters(IMethodSymbol method, WellKnownTypes wellKnownTypes)
    {
        if (method.Parameters.Length < 2 || method.Parameters.Length > 3)
        {
            return false;
        }

        var isFirstParameterString = method.Parameters.First().Type.SpecialType == SpecialType.System_String;
        var hasLastParameterOutModifier = method.Parameters.Last().RefKind == RefKind.Out;
        var isIFormatProviderParameterValidIfExist = method.Parameters.Length < 3 || wellKnownTypes.IFormatProvider.IsAssignableFrom(method.Parameters[1].Type);

        if (!isFirstParameterString || !hasLastParameterOutModifier || !isIFormatProviderParameterValidIfExist)
        {
            return false;
        }

        return true;
    }

    private static bool ValidateBindAsyncParameters(IMethodSymbol method, WellKnownTypes wellKnownTypes)
    {
        if (method.Parameters.Length < 1 || method.Parameters.Length > 2)
        {
            return false;
        }

        var isFirstParameterHttpContext = wellKnownTypes.HttpContext.IsAssignableFrom(method.Parameters[0].Type);
        var isSecondParameterParameterInfoIfExists = method.Parameters.Length < 2 || wellKnownTypes.ParameterInfo.IsAssignableFrom(method.Parameters[1].Type);
        if (!isFirstParameterHttpContext || !isSecondParameterParameterInfoIfExists)
        {
            return false;
        }

        return true;
    }

    private static bool ValidateCustomBindingModifiers(OperationAnalysisContext context, IMethodSymbol method)
    {
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CustomBindingMethodMustBePublic,
                method.Locations[0],
                method.Name));
            return false;
        }

        if (!method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CustomBindingMethodMustBeStatic,
                method.Locations[0],
                method.Name));
            return false;
        }

        return true;
    }

    private static void DetectInvalidMethodFormatFromParameter(
        OperationAnalysisContext context,
        IParameterSymbol parameterSymbol,
        string methodName,
        INamedTypeSymbol returnType,
        Func<OperationAnalysisContext, IMethodSymbol, bool> parametersValidator,
        Func<OperationAnalysisContext, IMethodSymbol, bool> modifiersValidator,
        DiagnosticDescriptor onInvalidMessage)
    {
        var bindAsyncMembers = GetAllMethods(parameterSymbol, methodName);

        if (bindAsyncMembers.Length == 0)
        {
            return;
        }

        foreach (var bindAsyncMember in bindAsyncMembers)
        {
            if (bindAsyncMember is IMethodSymbol method)
            {
                if(!ValidReturnType(returnType, method))
                {
                    continue;
                }

                if (!parametersValidator(context, method))
                {
                    continue;
                }

                if (modifiersValidator(context, method))
                {
                    return;
                }
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(
            onInvalidMessage,
            parameterSymbol.Locations[0],
            parameterSymbol.Type.Name));
    }

    private static bool ValidReturnType(INamedTypeSymbol returnType, IMethodSymbol method)
    {
        if (!returnType.IsGenericType)
        {
            return returnType.IsAssignableFrom(method.ReturnType);
        }

        if (method.ReturnType is INamedTypeSymbol namedMethodReturnType)
        {
            if (!namedMethodReturnType.IsGenericType)
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals(
                returnType.ConstructUnboundGenericType(),
                namedMethodReturnType.ConstructUnboundGenericType()))
            {
                return false;
            }
        }

        return true;
    }

    private static ImmutableArray<ISymbol> GetAllMethods(IParameterSymbol parameterSymbol, string methodName)
    {
        List<ISymbol> symbols = new (parameterSymbol.Type.GetMembers(methodName));

        var baseType = parameterSymbol.Type.BaseType;

        while (baseType != null)
        {
            symbols.AddRange(baseType.GetMembers(methodName));
            baseType = baseType.BaseType;
        }

        return symbols.ToImmutableArray();
    }
}
