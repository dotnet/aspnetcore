// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators;

internal static class ToolBlockParser
{
    private const string ToolBlockAttributeFullName = "Microsoft.AspNetCore.Components.AI.ToolBlockAttribute";
    private const string ToolParameterAttributeFullName = "Microsoft.AspNetCore.Components.AI.ToolParameterAttribute";
    private const string ToolResultAttributeFullName = "Microsoft.AspNetCore.Components.AI.ToolResultAttribute";
    private const string FunctionInvocationContentBlockFullName = "Microsoft.AspNetCore.Components.AI.FunctionInvocationContentBlock";

    public static ToolBlockCandidate? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;

        // Validate partial
        if (!classDecl.Modifiers.Any(m => m.Text == "partial"))
        {
            ctx.SemanticModel.Compilation.GetDiagnostics(ct);
            // Report in the generator output step instead
            return null;
        }

        // Validate not abstract
        if (classSymbol.IsAbstract)
        {
            return null;
        }

        // Validate not generic
        if (classSymbol.IsGenericType)
        {
            return null;
        }

        // Validate base class
        if (!ExtendsType(classSymbol, FunctionInvocationContentBlockFullName))
        {
            return null;
        }

        // Extract tool name from attribute
        string? toolName = null;
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ToolBlockAttributeFullName
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string name)
            {
                toolName = name;
                break;
            }
        }

        if (string.IsNullOrEmpty(toolName))
        {
            return null;
        }

        // Collect [ToolParameter] properties
        var parameters = new List<ToolParameterInfo>();
        var seenKeys = new Dictionary<string, string>();

        foreach (var member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol prop)
            {
                continue;
            }

            AttributeData? paramAttr = null;
            foreach (var attr in prop.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == ToolParameterAttributeFullName)
                {
                    paramAttr = attr;
                    break;
                }
            }

            if (paramAttr is null)
            {
                continue;
            }

            // Check setter
            if (prop.SetMethod is null)
            {
                continue;
            }

            // Get argument key
            string argKey = prop.Name;
            foreach (var namedArg in paramAttr.NamedArguments)
            {
                if (namedArg.Key == "Name" && namedArg.Value.Value is string nameOverride && !string.IsNullOrEmpty(nameOverride))
                {
                    argKey = nameOverride;
                    break;
                }
            }

            // Check for duplicate keys
            if (seenKeys.TryGetValue(argKey, out var existingProp))
            {
                continue;
            }

            seenKeys[argKey] = prop.Name;

            var typeKind = GetTypeKind(prop.Type);
            var isNullable = prop.Type.NullableAnnotation == NullableAnnotation.Annotated
                || prop.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            parameters.Add(new ToolParameterInfo
            {
                PropertyName = prop.Name,
                ArgumentKey = argKey,
                TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsNullable = isNullable,
                TypeKind = typeKind
            });
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // Collect [ToolResult] properties
        var resultProperties = new List<ToolResultPropertyInfo>();
        var seenResultKeys = new Dictionary<string, string>();

        foreach (var member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol prop)
            {
                continue;
            }

            AttributeData? resultAttr = null;
            foreach (var attr in prop.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == ToolResultAttributeFullName)
                {
                    resultAttr = attr;
                    break;
                }
            }

            if (resultAttr is null)
            {
                continue;
            }

            if (prop.SetMethod is null)
            {
                continue;
            }

            string resultKey = prop.Name;
            foreach (var namedArg in resultAttr.NamedArguments)
            {
                if (namedArg.Key == "Name" && namedArg.Value.Value is string nameOverride && !string.IsNullOrEmpty(nameOverride))
                {
                    resultKey = nameOverride;
                    break;
                }
            }

            if (seenResultKeys.TryGetValue(resultKey, out var existingProp2))
            {
                continue;
            }

            seenResultKeys[resultKey] = prop.Name;

            var resultTypeKind = GetTypeKind(prop.Type);
            var resultIsNullable = prop.Type.NullableAnnotation == NullableAnnotation.Annotated
                || prop.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            resultProperties.Add(new ToolResultPropertyInfo
            {
                PropertyName = prop.Name,
                ResultKey = resultKey,
                TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsNullable = resultIsNullable,
                TypeKind = resultTypeKind
            });
        }

        return new ToolBlockCandidate
        {
            Namespace = ns,
            ClassName = classSymbol.Name,
            ToolName = toolName!,
            Parameters = parameters,
            ResultProperties = resultProperties
        };
    }

    private static bool ExtendsType(INamedTypeSymbol symbol, string fullName)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == fullName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static ParameterTypeKind GetTypeKind(ITypeSymbol type)
    {
        // Unwrap Nullable<T>
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && type is INamedTypeSymbol namedType
            && namedType.TypeArguments.Length == 1)
        {
            type = namedType.TypeArguments[0];
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => ParameterTypeKind.String,
            SpecialType.System_Int32 => ParameterTypeKind.Int32,
            SpecialType.System_Int64 => ParameterTypeKind.Int64,
            SpecialType.System_Double => ParameterTypeKind.Double,
            SpecialType.System_Single => ParameterTypeKind.Single,
            SpecialType.System_Decimal => ParameterTypeKind.Decimal,
            SpecialType.System_Boolean => ParameterTypeKind.Boolean,
            _ => ParameterTypeKind.Complex
        };
    }
}
