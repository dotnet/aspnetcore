// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal sealed record MemberKey(
    string? DeclaringType,
    MemberType MemberKind,
    string? Name,
    string? ReturnType,
    List<string>? Parameters) : IEquatable<MemberKey>
{
    private static readonly SymbolDisplayFormat _withTypeParametersFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private static readonly SymbolDisplayFormat _withoutTypeParametersFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None);

    public static MemberKey? FromMethodSymbol(IMethodSymbol method)
    {
        string returnType;
        if (method.ReturnsVoid)
        {
            returnType = "typeof(void)";
        }
        else
        {
            var actualReturnType = method.ReturnType;
            if (actualReturnType.TypeKind == TypeKind.TypeParameter)
            {
                returnType = "typeof(object)";
            }
            else if (TryGetFormattedTypeName(actualReturnType, out var formattedReturnType))
            {
                returnType = $"typeof({formattedReturnType})";
            }
            else
            {
                return null;
            }
        }

        // Handle extension methods by skipping the 'this' parameter
        List<string> parameters = [];
        foreach (var parameter in method.Parameters)
        {
            if (parameter.IsThis)
            {
                continue;
            }

            if (parameter.Type.TypeKind == TypeKind.TypeParameter)
            {
                parameters.Add("typeof(object)");
            }
            else if (parameter.IsParams && parameter.Type is IArrayTypeSymbol arrayType)
            {
                if (TryGetFormattedTypeName(arrayType.ElementType, out var formattedArrayType))
                {
                    parameters.Add($"typeof({formattedArrayType}[])");
                }
                else
                {
                    return null;
                }
            }
            else if (TryGetFormattedTypeName(parameter.Type, out var formattedParameterType))
            {
                parameters.Add($"typeof({formattedParameterType})");
            }
            else
            {
                return null;
            }
        }

        if (TryGetFormattedTypeName(method.ContainingType, out var formattedDeclaringType))
        {
            return new MemberKey(
                $"typeof({formattedDeclaringType})",
                MemberType.Method,
                method.MetadataName,  // Use MetadataName to match runtime MethodInfo.Name
                returnType,
                parameters);
        }
        return null;
    }

    public static MemberKey? FromPropertySymbol(IPropertySymbol property)
    {
        if (TryGetFormattedTypeName(property.ContainingType, out var typeName))
        {
            return new MemberKey(
                $"typeof({typeName})",
                MemberType.Property,
                property.Name,
                null,
                null);
        }
        return null;
    }

    public static MemberKey? FromTypeSymbol(INamedTypeSymbol type)
    {
        if (TryGetFormattedTypeName(type, out var typeName))
        {
            return new MemberKey(
                $"typeof({typeName})",
                MemberType.Type,
                null,
                null,
                null);
        }
        return null;
    }

    /// Supports replacing generic type arguments to support use of open
    /// generics in `typeof` expressions for the declaring type.
    private static bool TryGetFormattedTypeName(ITypeSymbol typeSymbol, [NotNullWhen(true)] out string? typeName, bool isNestedCall = false)
    {
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            typeName = typeSymbol.ToDisplayString(_withTypeParametersFormat);
            return true;
        }

        // Handle tuples specially since they are represented as generic
        // ValueTuple types and trigger the logic for handling generics in
        // nested values.
        if (typeSymbol is INamedTypeSymbol { IsTupleType: true } namedType)
        {
            return TryHandleTupleType(namedType, out typeName);
        }

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } genericType)
        {
            // If any of the type arguments are type parameters, then they have not
            // been substituted for a concrete type and we need to model them as open
            // generics if possible to avoid emitting a type with type parameters that
            // cannot be used in a typeof expression.
            var hasTypeParameters = genericType.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
            var typeNameWithoutGenerics = genericType.ToDisplayString(_withoutTypeParametersFormat);

            if (!hasTypeParameters)
            {
                var typeArgStrings = new List<string>();
                var allArgumentsResolved = true;

                // Loop through each type argument to handle nested generics.
                foreach (var typeArg in genericType.TypeArguments)
                {
                    if (TryGetFormattedTypeName(typeArg, out var argTypeName, isNestedCall: true))
                    {
                        typeArgStrings.Add(argTypeName);
                    }
                    else
                    {
                        typeName = null;
                        return false;
                    }
                }

                if (allArgumentsResolved)
                {
                    typeName = $"{typeNameWithoutGenerics}<{string.Join(", ", typeArgStrings)}>";
                    return true;
                }
            }
            else
            {
                if (isNestedCall)
                {
                    // If this is a nested call, we can't use open generics so there's no way
                    // for us to emit a member key. Return false and skip over this type in the code
                    // generation.
                    typeName = null;
                    return false;
                }

                // If we got here, we can successfully emit a member key for the open generic type.
                var genericArgumentsCount = genericType.TypeArguments.Length;
                var openGenericsPlaceholder = "<" + new string(',', genericArgumentsCount - 1) + ">";

                typeName = typeNameWithoutGenerics + openGenericsPlaceholder;
                return true;
            }
        }

        typeName = typeSymbol.ToDisplayString(_withTypeParametersFormat);
        return true;
    }

    private static bool TryHandleTupleType(INamedTypeSymbol tupleType, [NotNullWhen(true)] out string? typeName)
    {
        List<string> elementTypes = [];
        foreach (var element in tupleType.TupleElements)
        {
            if (element.Type.TypeKind == TypeKind.TypeParameter)
            {
                elementTypes.Add("object");
            }
            else
            {
                // Process each tuple element and handle nested generics
                if (!TryGetFormattedTypeName(element.Type, out var elementTypeName, isNestedCall: true))
                {
                    typeName = null;
                    return false;
                }
                elementTypes.Add(elementTypeName);
            }
        }

        typeName = $"global::System.ValueTuple<{string.Join(", ", elementTypes)}>";
        return true;
    }
}

internal enum MemberType
{
    Type,
    Property,
    Method
}
