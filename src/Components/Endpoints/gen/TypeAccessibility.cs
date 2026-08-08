// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

// Whether generated code living in a given assembly can write a type's name. Everything the generator
// emits is ordinary C# compiled into the application, so a type it cannot name is a type it cannot
// describe — which is the boundary between a described component and one the framework reflects over.
internal static class TypeAccessibility
{
    public static bool IsNameable(ITypeSymbol type, IAssemblySymbol generatedIn)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return IsNameable(array.ElementType, generatedIn);

            case IPointerTypeSymbol:
            case IFunctionPointerTypeSymbol:
                return false;

            case ITypeParameterSymbol:
                return false;

            case IDynamicTypeSymbol:
                return true;

            case INamedTypeSymbol named:
                if (!IsAccessible(named, generatedIn))
                {
                    return false;
                }

                if (named.IsUnboundGenericType)
                {
                    return true;
                }

                foreach (var argument in named.TypeArguments)
                {
                    if (!IsNameable(argument, generatedIn))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return false;
        }
    }

    private static bool IsAccessible(INamedTypeSymbol type, IAssemblySymbol generatedIn)
    {
        var isOwnAssembly = SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, generatedIn) ||
            type.ContainingAssembly is null ||
            type.ContainingAssembly.GivesAccessTo(generatedIn);

        for (var current = type; current is not null; current = current.ContainingType)
        {
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    break;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    if (!isOwnAssembly)
                    {
                        return false;
                    }

                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    // A member the generated code can touch directly with an ordinary member access.
    public static bool IsDirectlyAccessible(ISymbol member, IAssemblySymbol generatedIn)
    {
        var isOwnAssembly = SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, generatedIn) ||
            member.ContainingAssembly is null ||
            member.ContainingAssembly.GivesAccessTo(generatedIn);

        return member.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Internal or Accessibility.ProtectedOrInternal => isOwnAssembly,
            _ => false,
        };
    }
}
