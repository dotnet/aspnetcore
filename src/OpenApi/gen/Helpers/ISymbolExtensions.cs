// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal static class ISymbolExtensions
{
    public static ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations(this ISymbol symbol)
    {
        if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
        {
            return [];
        }

        var result = ImmutableArray.CreateBuilder<ISymbol>();

        foreach (var iface in symbol.ContainingType.AllInterfaces)
        {
            foreach (var interfaceMember in iface.GetMembers())
            {
                var impl = symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEqualityComparer.Default.Equals(symbol, impl))
                {
                    result.Add(interfaceMember);
                }
            }
        }

        // There are explicit methods that FindImplementationForInterfaceMember.  For example `abstract explicit impls`
        // like `abstract void I<T>.M()`.  So add these back in directly using symbol.ExplicitInterfaceImplementations.
        result.AddRange(symbol.ExplicitInterfaceImplementations());

        return result.ToImmutable();
    }

    public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
        => symbol switch
        {
            IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
            IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
            IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
            _ => [],
        };

    public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this ISymbol? symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeParameterSymbol>();

        while (symbol != null)
        {
            results.AddRange(symbol.GetTypeParameters());
            symbol = symbol.ContainingType;
        }

        return results.ToImmutable();
    }

    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
        => symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol nt => nt.TypeParameters,
            _ => [],
        };

    public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeSymbol>();
        results.AddRange(symbol.GetTypeArguments());

        var containingType = symbol.ContainingType;
        while (containingType != null)
        {
            results.AddRange(containingType.GetTypeArguments());
            containingType = containingType.ContainingType;
        }

        return results.ToImmutable();
    }

    public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol)
        => symbol switch
        {
            IMethodSymbol m => m.TypeArguments,
            INamedTypeSymbol nt => nt.TypeArguments,
            _ => [],
        };

    public static ISymbol? GetOverriddenMember(this ISymbol? symbol, bool allowLooseMatch = false)
    {
        if (symbol is null)
        {
            return null;
        }

        ISymbol? exactMatch = symbol switch
        {
            IMethodSymbol method => method.OverriddenMethod,
            IPropertySymbol property => property.OverriddenProperty,
            IEventSymbol @event => @event.OverriddenEvent,
            _ => null,
        };

        if (exactMatch != null)
        {
            return exactMatch;
        }

        if (allowLooseMatch &&
            (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride))
        {
            foreach (var baseType in symbol.ContainingType.GetBaseTypes())
            {
                if (TryFindLooseMatch(symbol, baseType, out var looseMatch))
                {
                    return looseMatch;
                }
            }
        }

        return null;

        static bool TryFindLooseMatch(ISymbol symbol, INamedTypeSymbol baseType, [NotNullWhen(true)] out ISymbol? looseMatch)
        {
            IMethodSymbol? bestMethod = null;
            var parameterCount = symbol.GetParameters().Length;

            foreach (var member in baseType.GetMembers(symbol.Name))
            {
                if (member.Kind != symbol.Kind)
                {
                    continue;
                }

                if (!member.IsOverridable())
                {
                    continue;
                }

                if (symbol.Kind is SymbolKind.Event or SymbolKind.Property)
                {
                    // We've found a matching event/property in the base type (perhaps differing by return type). This
                    // is a good enough match to return as a loose match for the starting symbol.
                    looseMatch = member;
                    return true;
                }
                else if (member is IMethodSymbol method)
                {
                    // Prefer methods that are closed in parameter count to the original method we started with.
                    if (bestMethod is null || Math.Abs(method.Parameters.Length - parameterCount) < Math.Abs(bestMethod.Parameters.Length - parameterCount))
                    {
                        bestMethod = method;
                    }
                }
            }

            looseMatch = bestMethod;
            return looseMatch != null;
        }
    }

    public static bool IsOverridable([NotNullWhen(true)] this ISymbol? symbol)
    {
        // Members can only have overrides if they are virtual, abstract or override and is not sealed.
        return symbol is { ContainingType.TypeKind: TypeKind.Class, IsSealed: false } &&
               (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride);
    }

    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        => symbol switch
        {
            IMethodSymbol m => m.Parameters,
            IPropertySymbol nt => nt.Parameters,
            _ => [],
        };

    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol? type)
    {
        var current = type?.BaseType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
