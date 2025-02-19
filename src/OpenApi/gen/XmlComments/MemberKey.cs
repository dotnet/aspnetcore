// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal sealed record MemberKey(
    string? DeclaringType,
    MemberType MemberKind,
    string? Name,
    string? ReturnType,
    string[]? Parameters) : IEquatable<MemberKey>
{
    private static readonly SymbolDisplayFormat _typeKeyFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static MemberKey FromMethodSymbol(IMethodSymbol method, Compilation compilation)
    {
        string returnType;
        if (method.ReturnsVoid)
        {
            returnType = "typeof(void)";
        }
        else
        {
            // Handle Task/ValueTask for async methods
            var actualReturnType = method.ReturnType;
            if (method.IsAsync && actualReturnType is INamedTypeSymbol namedType)
            {
                if (namedType.TypeArguments.Length > 0)
                {
                    actualReturnType = namedType.TypeArguments[0];
                }
                else
                {
                    actualReturnType = compilation.GetSpecialType(SpecialType.System_Void);
                }
            }

            returnType = actualReturnType.TypeKind == TypeKind.TypeParameter
                ? "typeof(object)"
                : $"typeof({actualReturnType.ToDisplayString(_typeKeyFormat)})";
        }

        // Handle extension methods by skipping the 'this' parameter
        var parameters = method.Parameters
            .Where(p => !p.IsThis)
            .Select(p =>
            {
                if (p.Type.TypeKind == TypeKind.TypeParameter)
                {
                    return "typeof(object)";
                }

                // For params arrays, use the array type
                if (p.IsParams && p.Type is IArrayTypeSymbol arrayType)
                {
                    return $"typeof({arrayType.ToDisplayString(_typeKeyFormat)})";
                }

                return $"typeof({p.Type.ToDisplayString(_typeKeyFormat)})";
            })
            .ToArray();

        // For generic methods, use the containing type with generic parameters
        var declaringType = method.ContainingType;
        var typeDisplay = declaringType.ToDisplayString(_typeKeyFormat);

        // If the method is in a generic type, we need to handle the type parameters
        if (declaringType.IsGenericType)
        {
            typeDisplay = ReplaceGenericArguments(typeDisplay);
        }

        return new MemberKey(
            $"typeof({typeDisplay})",
            MemberType.Method,
            method.MetadataName,  // Use MetadataName to match runtime MethodInfo.Name
            returnType,
            parameters);
    }

    public static MemberKey FromPropertySymbol(IPropertySymbol property)
    {
        return new MemberKey(
            $"typeof({ReplaceGenericArguments(property.ContainingType.ToDisplayString(_typeKeyFormat))})",
            MemberType.Property,
            property.Name,
            null,
            null);
    }

    public static MemberKey FromTypeSymbol(INamedTypeSymbol type)
    {
        return new MemberKey(
            $"typeof({ReplaceGenericArguments(type.ToDisplayString(_typeKeyFormat))})",
            MemberType.Type,
            null,
            null,
            null);
    }

    /// Supports replacing generic type arguments to support use of open
    /// generics in `typeof` expressions for the declaring type.
    private static string ReplaceGenericArguments(string typeName)
    {
        var stack = new Stack<int>();
        var result = new StringBuilder(typeName);
        for (var i = 0; i < result.Length; i++)
        {
            if (result[i] == '<')
            {
                stack.Push(i);
            }
            else if (result[i] == '>' && stack.Count > 0)
            {
                var start = stack.Pop();
                // Replace everything between < and > with empty strings separated by commas
                var segment = result.ToString(start + 1, i - start - 1);
                var commaCount = segment.Count(c => c == ',');
                var replacement = new string(',', commaCount);
                result.Remove(start + 1, i - start - 1);
                result.Insert(start + 1, replacement);
                i = start + replacement.Length + 1;
            }
        }
        return result.ToString();
    }
}

internal enum MemberType
{
    Type,
    Property,
    Method
}
