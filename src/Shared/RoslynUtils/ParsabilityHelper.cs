// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

internal static class ParsabilityHelper
{
    private static readonly BoundedCacheWithFactory<ITypeSymbol, (BindabilityMethod?, IMethodSymbol?)> BindabilityCache = new();
    private static readonly BoundedCacheWithFactory<ITypeSymbol, (Parsability, ParsabilityMethod?)> ParsabilityCache = new();

    private static bool IsTypeAlwaysParsable(ITypeSymbol typeSymbol, [NotNullWhen(true)] out ParsabilityMethod? parsabilityMethod)
    {
        // Any enum is valid.
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            parsabilityMethod = ParsabilityMethod.Enum;
            return true;
        }

        // Uri is valid.
        if (typeSymbol.EqualsByName(["System", "Uri"]))
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

    internal static Parsability GetParsability(ITypeSymbol typeSymbol)
    {
        return GetParsability(typeSymbol, out var _);
    }

    internal static Parsability GetParsability(ITypeSymbol typeSymbol, [NotNullWhen(false)] out ParsabilityMethod? parsabilityMethod)
    {
        var parsability = Parsability.NotParsable;
        parsabilityMethod = null;

        (parsability, parsabilityMethod) = ParsabilityCache.GetOrCreateValue(typeSymbol, (typeSymbol) =>
        {
            if (IsTypeAlwaysParsable(typeSymbol, out var parsabilityMethod))
            {
                return (Parsability.Parsable, parsabilityMethod);
            }

            // MyType : IParsable<MyType>()
            if (IsParsableViaIParsable(typeSymbol))
            {
                return (Parsability.Parsable, ParsabilityMethod.IParsable);
            }

            // Check if the parameter type has a public static TryParse method.
            var tryParseMethods = typeSymbol.GetThisAndBaseTypes()
                .SelectMany(t => t.GetMembers("TryParse"))
                .OfType<IMethodSymbol>();

            if (tryParseMethods.Any(m => IsTryParseWithFormat(m)))
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

    private static bool IsTryParseWithFormat(IMethodSymbol methodSymbol)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
            methodSymbol.Parameters.Length == 3 &&
            methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
            methodSymbol.Parameters[1].Type.EqualsByName(["System", "IFormatProvider"]) &&
            methodSymbol.Parameters[2].RefKind == RefKind.Out;
    }

    internal static bool IsParsableViaIParsable(ITypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(i => i.ConstructedFrom.EqualsByName(["System", "IParsable"]));

    private static bool IsBindableViaIBindableFromHttpContext(ITypeSymbol typeSymbol)
    {
        var constructedTypeSymbol = typeSymbol.AllInterfaces.FirstOrDefault(
            i => i.ConstructedFrom.EqualsByName(["Microsoft", "AspNetCore", "Http", "IBindableFromHttpContext"])
            );
        return constructedTypeSymbol != null &&
            SymbolEqualityComparer.Default.Equals(constructedTypeSymbol.TypeArguments[0].UnwrapTypeSymbol(unwrapNullable: true), typeSymbol);
    }

    private static bool IsBindAsync(IMethodSymbol methodSymbol, ITypeSymbol typeSymbol)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 1 &&
            methodSymbol.Parameters[0].Type.EqualsByName(["Microsoft", "AspNetCore", "Http", "HttpContext"]) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            returnType.IsGenericType &&
            returnType.ConstructedFrom.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], typeSymbol);
    }

    private static bool IsBindAsyncWithParameter(IMethodSymbol methodSymbol, ITypeSymbol typeSymbol)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
            methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 2 &&
            methodSymbol.Parameters[0].Type.EqualsByName(["Microsoft", "AspNetCore", "Http", "HttpContext"]) &&
            methodSymbol.Parameters[1].Type.EqualsByName(["System", "Reflection", "ParameterInfo"]) &&
            methodSymbol.ReturnType is INamedTypeSymbol returnType &&
            IsReturningValueTaskOfTOrNullableT(returnType, typeSymbol);
    }

    private static bool IsReturningValueTaskOfTOrNullableT(INamedTypeSymbol returnType, ITypeSymbol containingType)
    {
        return returnType.IsGenericType && returnType.ConstructedFrom.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]) &&
            SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0].UnwrapTypeSymbol(unwrapNullable: true), containingType);
    }

    internal static Bindability GetBindability(ITypeSymbol typeSymbol, out BindabilityMethod? bindabilityMethod, out IMethodSymbol? bindMethodSymbol)
    {
        bindabilityMethod = null;
        bindMethodSymbol = null;
        IMethodSymbol? bindAsyncMethod = null;

        (bindabilityMethod, bindMethodSymbol) = BindabilityCache.GetOrCreateValue(typeSymbol, (typeSymbol) =>
        {
            BindabilityMethod? bindabilityMethod = null;
            IMethodSymbol? bindMethodSymbol = null;
            if (IsBindableViaIBindableFromHttpContext(typeSymbol))
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
                        if (IsBindAsyncWithParameter(methodSymbol, typeSymbol))
                        {
                            bindabilityMethod = BindabilityMethod.BindAsyncWithParameter;
                            bindMethodSymbol = methodSymbol;
                            break;
                        }
                        if (IsBindAsync(methodSymbol, typeSymbol))
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
            if (bindAsyncMethod.ReturnType is INamedTypeSymbol returnType && !IsReturningValueTaskOfTOrNullableT(returnType, typeSymbol))
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
