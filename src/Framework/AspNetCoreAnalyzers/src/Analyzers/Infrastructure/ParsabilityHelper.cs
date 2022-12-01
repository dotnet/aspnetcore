// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

internal static class ParsabilityHelper
{
    private static bool IsTypeAlwaysParsableOrBindable(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        // Any enum is valid.
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        // Uri is valid.
        if (SymbolEqualityComparer.Default.Equals(typeSymbol, wellKnownTypes.Get(WellKnownType.System_Uri)))
        {
            return true;
        }

        // Strings are valid.
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        return false;
    }

    internal static bool IsTypeParsable(INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (IsTypeAlwaysParsableOrBindable(typeSymbol, wellKnownTypes))
        {
            return true;
        }

        // MyType : IParsable<MyType>()
        if (IsParsableViaIParsable(typeSymbol))
        {
            return true;
        }

        // Check if the parameter type has a public static TryParse method.
        foreach (var item in typeSymbol.GetMembers("TryParse"))
        {
            // bool TryParse(string input, out T value)
            if (IsTryParse(item))
            {
                return true;
            }

            // bool TryParse(string input, IFormatProvider provider, out T value)
            if (IsTryParseWithFormat(item, wellKnownTypes))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTryParse(ISymbol item)
    {
        return item is IMethodSymbol methodSymbol &&
            methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.Length == 2 &&
            methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
            methodSymbol.Parameters[1].RefKind == RefKind.Out;
    }

    private static bool IsTryParseWithFormat(ISymbol item, WellKnownTypes wellKnownTypes)
    {
        return item is IMethodSymbol methodSymbol &&
            methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.Length == 3 &&
            methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[1].Type, wellKnownTypes.Get(WellKnownType.System_IFormatProvider)) &&
            methodSymbol.Parameters[2].RefKind == RefKind.Out;
    }

    private static bool IsParsableViaIParsable(ITypeSymbol typeSymbol)
    {
        var implementsIParsable = typeSymbol.AllInterfaces.Any(
            i => i.Name == "IParsable" && // ... implements IParsable
                 i.TypeArguments.Length == 1 && // ... being pedantic that it has just one type argument.
                 i.TypeArguments.All(t => t.Name == typeSymbol.Name)); // ... and the type argument is the same as the type symbol

        return implementsIParsable;
    }

    private static bool IsBindableViaIBindableFromHttpContext(ITypeSymbol typeSymbol)
    {
        var implementsIBindableFromHttpContext = typeSymbol.AllInterfaces.Any(
            i => i.Name == "IBindableFromHttpContext" && // ... implements IParsable
                 i.TypeArguments.Length == 1 && // ... being pedantic that it has just one type argument.
                 i.TypeArguments.All(t => t.Name == typeSymbol.Name)); // ... and the type argument is the same as the type symbol

        return implementsIBindableFromHttpContext;
    }

    private static bool IsBindAsync(IMethodSymbol methodSymbol, ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            SymbolEqualityComparer.Default.Equals(returnType.ConstructedFrom, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T)) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], typeSymbol);
    }

    private static bool IsBindAsyncWithParameter(IMethodSymbol methodSymbol, ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 2 &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)) &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[1].Type, wellKnownTypes.Get(WellKnownType.System_Reflection_ParameterInfo)) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            SymbolEqualityComparer.Default.Equals(returnType.ConstructedFrom, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T)) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], typeSymbol);
    }

    internal static bool IsTypeBindable(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (IsTypeAlwaysParsableOrBindable(typeSymbol, wellKnownTypes))
        {
            return true;
        }

        if (IsBindableViaIBindableFromHttpContext(typeSymbol))
        {
            return true;
        }

        var bindAsyncMethods = typeSymbol.GetMembers("BindAsync").OfType<IMethodSymbol>();
        foreach (var methodSymbol in bindAsyncMethods)
        {
            if (IsBindAsync(methodSymbol, typeSymbol, wellKnownTypes) || IsBindAsyncWithParameter(methodSymbol, typeSymbol, wellKnownTypes))
            {
                return true;
            }
        }

        return false;
    }
}

