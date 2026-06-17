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
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

using WellKnownType = WellKnownTypeData.WellKnownType;

internal static class ParsabilityHelper
{
    private static readonly BoundedCacheWithFactory<ITypeSymbol, (BindabilityMethod?, IMethodSymbol?)> BindabilityCache = new();
    private static readonly BoundedCacheWithFactory<ITypeSymbol, (Parsability, ParsabilityMethod?)> ParsabilityCache = new();

    private static bool IsTypeAlwaysParsable(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out ParsabilityMethod? parsabilityMethod)
    {
        // Any enum is valid.
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            parsabilityMethod = ParsabilityMethod.Enum;
            return true;
        }

        // Uri is valid.
        if (SymbolEqualityComparer.Default.Equals(typeSymbol, wellKnownTypes.Get(WellKnownType.System_Uri)))
        {
            parsabilityMethod = ParsabilityMethod.Uri;
            return true;
        }

        // Strings are valid.
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            parsabilityMethod = ParsabilityMethod.String;
            return true;
        }

        parsabilityMethod = null;
        return false;
    }

    internal static Parsability GetParsability(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        return GetParsability(typeSymbol, wellKnownTypes, out var _);
    }

    internal static Parsability GetParsability(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, [NotNullWhen(false)] out ParsabilityMethod? parsabilityMethod)
    {
        var parsability = Parsability.NotParsable;
        parsabilityMethod = null;

        (parsability, parsabilityMethod) = ParsabilityCache.GetOrCreateValue(typeSymbol, (typeSymbol) =>
        {
            if (IsTypeAlwaysParsable(typeSymbol, wellKnownTypes, out var parsabilityMethod))
            {
                return (Parsability.Parsable, parsabilityMethod);
            }

            // MyType : IParsable<MyType>()
            if (IsParsableViaIParsable(typeSymbol, wellKnownTypes))
            {
                return (Parsability.Parsable, ParsabilityMethod.IParsable);
            }

            // Check if the parameter type has a public static TryParse method.
            var tryParseMethods = typeSymbol.GetThisAndBaseTypes()
                .SelectMany(t => t.GetMembers("TryParse"))
                .OfType<IMethodSymbol>();

            if (tryParseMethods.Any(m => IsTryParseWithFormat(m, wellKnownTypes)))
            {
                return (Parsability.Parsable, ParsabilityMethod.TryParseWithFormatProvider);
            }

            if (tryParseMethods.Any(IsTryParse))
            {
                return (Parsability.Parsable, ParsabilityMethod.TryParse);
            }

            return (Parsability.NotParsable, null);
        });

        return parsability;
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

    internal static bool IsParsableViaIParsable(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
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
        var constructedTypeSymbol = typeSymbol.AllInterfaces.FirstOrDefault(
            i => SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iBindableFromHttpContextTypeSymbol)
            );
        return constructedTypeSymbol != null &&
            SymbolEqualityComparer.Default.Equals(constructedTypeSymbol.TypeArguments[0].UnwrapTypeSymbol(unwrapNullable: true), typeSymbol);
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
            IsReturningValueTaskOfTOrNullableT(returnType, typeSymbol, wellKnownTypes);
    }

    private static bool IsReturningValueTaskOfTOrNullableT(INamedTypeSymbol returnType, ITypeSymbol containingType, WellKnownTypes wellKnownTypes)
    {
        return SymbolEqualityComparer.Default.Equals(returnType.ConstructedFrom, wellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T)) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0].UnwrapTypeSymbol(unwrapNullable: true), containingType);
    }

    internal static Bindability GetBindability(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, out BindabilityMethod? bindabilityMethod, out IMethodSymbol? bindMethodSymbol)
    {
        bindabilityMethod = null;
        bindMethodSymbol = null;
        IMethodSymbol? bindAsyncMethod = null;

        (bindabilityMethod, bindMethodSymbol) = BindabilityCache.GetOrCreateValue(typeSymbol, (typeSymbol) =>
        {
            BindabilityMethod? bindabilityMethod = null;
            IMethodSymbol? bindMethodSymbol = null;
            if (IsBindableViaIBindableFromHttpContext(typeSymbol, wellKnownTypes))
            {
                return (BindabilityMethod.IBindableFromHttpContext, null);
            }

            var searchCandidates = typeSymbol.GetThisAndBaseTypes()
                .Concat(typeSymbol.AllInterfaces);

            foreach (var candidate in searchCandidates)
            {
                var baseTypeBindAsyncMethods = candidate.GetMembers("BindAsync");
                foreach (var methodSymbolCandidate in baseTypeBindAsyncMethods)
                {
                    if (methodSymbolCandidate is IMethodSymbol methodSymbol)
                    {
                        bindAsyncMethod ??= methodSymbol;
                        if (IsBindAsyncWithParameter(methodSymbol, typeSymbol, wellKnownTypes))
                        {
                            bindabilityMethod = BindabilityMethod.BindAsyncWithParameter;
                            bindMethodSymbol = methodSymbol;
                            break;
                        }
                        if (IsBindAsync(methodSymbol, typeSymbol, wellKnownTypes))
                        {
                            bindabilityMethod = BindabilityMethod.BindAsync;
                            bindMethodSymbol = methodSymbol;
                        }
                    }
                }
            }

            return (bindabilityMethod, bindAsyncMethod);
        });

        if (bindabilityMethod is not null)
        {
            return Bindability.Bindable;
        }

        // See if we can give better guidance on why the BindAsync method is no good.
        if (bindAsyncMethod is not null)
        {
            if (bindAsyncMethod.ReturnType is INamedTypeSymbol returnType && !IsReturningValueTaskOfTOrNullableT(returnType, typeSymbol, wellKnownTypes))
            {
                return Bindability.InvalidReturnType;
            }
        }

        return Bindability.NotBindable;
    }
}

internal enum Parsability
{
    Parsable,
    NotParsable,
}

internal enum ParsabilityMethod
{
    String,
    IParsable,
    Enum,
    TryParse,
    TryParseWithFormatProvider,
    Uri,
}

internal enum Bindability
{
    Bindable,
    NotBindable,
    InvalidReturnType,
}

internal enum BindabilityMethod
{
    IBindableFromHttpContext,
    BindAsync,
    BindAsyncWithParameter,
}
