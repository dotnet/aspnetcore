// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

// Rebuilds an attribute instance as C#. The descriptors carry attribute instances rather than a
// projection of them, so that a new routing or cascading-parameter attribute needs no change to the
// generator, the descriptors, or the framework code that reads them.
internal static class AttributeExpressionWriter
{
    public static bool TryWrite(AttributeData attribute, IAssemblySymbol generatedIn, out string expression)
    {
        expression = string.Empty;

        if (attribute.AttributeClass is not { } attributeClass ||
            !TypeAccessibility.IsNameable(attributeClass, generatedIn))
        {
            return false;
        }

        var constructor = attribute.AttributeConstructor;
        if (constructor is null || constructor.DeclaredAccessibility is not Accessibility.Public)
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.Append("new ").Append(attributeClass.FullName()).Append('(');

        for (var i = 0; i < attribute.ConstructorArguments.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            if (!TryWriteConstant(attribute.ConstructorArguments[i], generatedIn, builder))
            {
                return false;
            }
        }

        builder.Append(')');

        if (attribute.NamedArguments.Length > 0)
        {
            builder.Append(" { ");
            for (var i = 0; i < attribute.NamedArguments.Length; i++)
            {
                var named = attribute.NamedArguments[i];

                // A named argument can target a read-only member on a base type the generated code
                // cannot set; only settable members declared as properties are reproducible.
                if (!IsWritableNamedArgument(attributeClass, named.Key))
                {
                    return false;
                }

                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(named.Key).Append(" = ");
                if (!TryWriteConstant(named.Value, generatedIn, builder))
                {
                    return false;
                }
            }

            builder.Append(" }");
        }

        expression = builder.ToString();
        return true;
    }

    private static bool IsWritableNamedArgument(INamedTypeSymbol attributeClass, string name)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                switch (member)
                {
                    case IPropertySymbol { SetMethod: { DeclaredAccessibility: Accessibility.Public } }:
                    case IFieldSymbol { IsReadOnly: false, IsConst: false, DeclaredAccessibility: Accessibility.Public }:
                        return true;
                    default:
                        return false;
                }
            }
        }

        return false;
    }

    private static bool TryWriteConstant(TypedConstant constant, IAssemblySymbol generatedIn, StringBuilder builder)
    {
        if (constant.IsNull)
        {
            builder.Append(constant.Type is null ? "null" : $"default({constant.Type.FullName()})");
            return true;
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return TryWritePrimitive(constant, builder);

            case TypedConstantKind.Enum:
                if (constant.Type is null)
                {
                    return false;
                }

                builder.Append('(').Append(constant.Type.FullName()).Append(')')
                    .Append(FormatNumber(constant.Value));
                return true;

            case TypedConstantKind.Type:
                if (constant.Value is not ITypeSymbol type || !TypeAccessibility.IsNameable(type, generatedIn))
                {
                    return false;
                }

                builder.Append("typeof(").Append(type.FullName()).Append(')');
                return true;

            case TypedConstantKind.Array:
                if (constant.Type is not IArrayTypeSymbol arrayType ||
                    !TypeAccessibility.IsNameable(arrayType.ElementType, generatedIn))
                {
                    return false;
                }

                builder.Append("new ").Append(arrayType.ElementType.FullName()).Append("[] { ");
                for (var i = 0; i < constant.Values.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    if (!TryWriteConstant(constant.Values[i], generatedIn, builder))
                    {
                        return false;
                    }
                }

                builder.Append(" }");
                return true;

            default:
                return false;
        }
    }

    private static bool TryWritePrimitive(TypedConstant constant, StringBuilder builder)
    {
        switch (constant.Value)
        {
            case string s:
                builder.Append(SymbolHelpers.ToStringLiteral(s));
                return true;
            case bool b:
                builder.Append(b ? "true" : "false");
                return true;
            case char c:
                builder.Append('\'').Append(c == '\'' ? "\\'" : c == '\\' ? "\\\\" : c.ToString()).Append('\'');
                return true;
            case float f:
                builder.Append(f.ToString("R", CultureInfo.InvariantCulture)).Append('f');
                return true;
            case double d:
                builder.Append(d.ToString("R", CultureInfo.InvariantCulture)).Append('d');
                return true;
            case decimal m:
                builder.Append(m.ToString(CultureInfo.InvariantCulture)).Append('m');
                return true;
            case null:
                builder.Append("null");
                return true;
            default:
                builder.Append(FormatNumber(constant.Value));
                return true;
        }
    }

    private static string FormatNumber(object? value)
        => value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? "0";
}
