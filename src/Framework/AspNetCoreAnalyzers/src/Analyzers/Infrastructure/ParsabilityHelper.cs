// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using System.ComponentModel;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

using WellKnownType = WellKnownTypeData.WellKnownType;

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

    internal static Parsability GetParsability(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (IsTypeAlwaysParsableOrBindable(typeSymbol, wellKnownTypes))
        {
            return Parsability.Parsable;
        }

        // MyType : IParsable<MyType>()
        if (IsParsableViaIParsable(typeSymbol, wellKnownTypes))
        {
            return Parsability.Parsable;
        }

        // Check if the parameter type has a public static TryParse method.
        var tryParseMethods = typeSymbol.GetMembers("TryParse").OfType<IMethodSymbol>();
        foreach (var tryParseMethodSymbol in tryParseMethods)
        {
            if (IsTryParse(tryParseMethodSymbol) || IsTryParseWithFormat(tryParseMethodSymbol, wellKnownTypes))
            {
                return Parsability.Parsable;
            }
        }

        return Parsability.NotParsable;
    }

    private static bool IsTryParse(IMethodSymbol methodSymbol)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.Length == 2 &&
            methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
            methodSymbol.Parameters[1].RefKind == RefKind.Out;
    }

    private static bool IsTryParseWithFormat(IMethodSymbol methodSymbol, WellKnownTypes wellKnownTypes)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.Length == 3 &&
            methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[1].Type, wellKnownTypes.Get(WellKnownType.System_IFormatProvider)) &&
            methodSymbol.Parameters[2].RefKind == RefKind.Out;
    }

    private static bool IsParsableViaIParsable(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        var iParsableTypeSymbol = wellKnownTypes.Get(WellKnownType.System_IParsable_T);
        var implementsIParsable = typeSymbol.AllInterfaces.Any(
            i => SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iParsableTypeSymbol)
            );
        return implementsIParsable;
    }

    private static bool IsBindableViaIBindableFromHttpContext(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        var iBindableFromHttpContextTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IBindableFromHttpContext_T);
        var implementsIBindableFromHttpContext = typeSymbol.AllInterfaces.Any(
            i => SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iBindableFromHttpContextTypeSymbol)
            );
        return implementsIBindableFromHttpContext;
    }

    private static bool IsBindAsync(IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            SymbolEqualityComparer.Default.Equals(returnType.ConstructedFrom, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T)) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], typeSymbol);
    }

    private static bool IsBindAsyncWithParameter(IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 2 &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)) &&
            SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[1].Type, wellKnownTypes.Get(WellKnownType.System_Reflection_ParameterInfo)) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            IsReturningValueTaskOfT(returnType, typeSymbol, wellKnownTypes);
    }

    private static bool IsReturningValueTaskOfT(INamedTypeSymbol returnType, INamedTypeSymbol containingType, WellKnownTypes wellKnownTypes)
    {
        return SymbolEqualityComparer.Default.Equals(returnType.ConstructedFrom, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T)) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], containingType);
    }

    internal static Bindability GetBindability(INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (IsTypeAlwaysParsableOrBindable(typeSymbol, wellKnownTypes))
        {
            return Bindability.Bindable;
        }

        if (IsBindableViaIBindableFromHttpContext(typeSymbol, wellKnownTypes))
        {
            return Bindability.Bindable;
        }

        var bindAsyncMethods = typeSymbol.GetMembers("BindAsync").OfType<IMethodSymbol>();
        foreach (var methodSymbol in bindAsyncMethods)
        {
            if (IsBindAsync(methodSymbol, typeSymbol, wellKnownTypes) || IsBindAsyncWithParameter(methodSymbol, typeSymbol, wellKnownTypes))
            {
                return Bindability.Bindable;
            }
        }

        // See if we can give better guidance on why the BindAsync method is no good.
        if (bindAsyncMethods.Count() == 1)
        {
            var bindAsyncMethod = bindAsyncMethods.Single();

            if (bindAsyncMethod.ReturnType is INamedTypeSymbol returnType && !IsReturningValueTaskOfT(returnType, typeSymbol, wellKnownTypes))
            {
                return Bindability.InvalidReturnType;
            }

        }

        return Bindability.NotBindable;
    }
}

