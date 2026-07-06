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

    private static readonly SymbolDisplayFormat s_escapedNamespaceFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public static ToolBlockParseResult Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var diagnostics = new List<DiagnosticInfo>();

        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;
        var location = LocationInfo.From(classDecl.Identifier.GetLocation());
        var displayName = classSymbol.Name;

        // Validate partial
        if (!classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.NotPartial.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        // Nested types cannot be represented by the (namespace + simple name) model and
        // would otherwise emit uncompilable code, so diagnose and skip them.
        if (classSymbol.ContainingType is not null)
        {
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.NestedType.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        // Validate not abstract
        if (classSymbol.IsAbstract)
        {
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.IsAbstract.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        // Validate not generic
        if (classSymbol.IsGenericType)
        {
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.IsGeneric.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        // Validate base class
        if (!ExtendsType(classSymbol, FunctionInvocationContentBlockFullName))
        {
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.WrongBaseClass.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
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
            diagnostics.Add(new DiagnosticInfo(DiagnosticDescriptors.EmptyToolName.Id, location, displayName));
            return new ToolBlockParseResult(candidate: null, diagnostics);
        }

        var parameters = ParseParameters(classSymbol, diagnostics, ct);
        var resultProperties = ParseResultProperties(classSymbol, ct);

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString(s_escapedNamespaceFormat);

        var candidate = new ToolBlockCandidate(
            @namespace: ns,
            className: classSymbol.Name,
            blockTypeGlobal: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            toolName: toolName!,
            parameters: parameters,
            resultProperties: resultProperties);

        return new ToolBlockParseResult(candidate, diagnostics);
    }

    private static List<ToolParameterInfo> ParseParameters(
        INamedTypeSymbol classSymbol, List<DiagnosticInfo> diagnostics, CancellationToken ct)
    {
        var parameters = new List<ToolParameterInfo>();
        var seenKeys = new Dictionary<string, string>();

        foreach (var member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol prop || !HasAttribute(prop, ToolParameterAttributeFullName, out var paramAttr))
            {
                continue;
            }

            if (prop.SetMethod is null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.PropertyNoSetter.Id, LocationInfo.From(prop.Locations.FirstOrDefault() ?? Location.None), prop.Name));
                continue;
            }

            var argKey = GetKeyOverride(paramAttr!, prop.Name);

            if (seenKeys.ContainsKey(argKey))
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.DuplicateArgumentKey.Id, LocationInfo.From(prop.Locations.FirstOrDefault() ?? Location.None), argKey));
                continue;
            }

            seenKeys[argKey] = prop.Name;

            parameters.Add(new ToolParameterInfo(
                propertyName: prop.Name,
                argumentKey: argKey,
                typeName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                isNullable: IsNullable(prop.Type),
                typeKind: GetTypeKind(prop.Type)));
        }

        return parameters;
    }

    private static List<ToolResultPropertyInfo> ParseResultProperties(INamedTypeSymbol classSymbol, CancellationToken ct)
    {
        var resultProperties = new List<ToolResultPropertyInfo>();
        var seenResultKeys = new Dictionary<string, string>();

        foreach (var member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol prop || !HasAttribute(prop, ToolResultAttributeFullName, out var resultAttr))
            {
                continue;
            }

            if (prop.SetMethod is null)
            {
                continue;
            }

            var resultKey = GetKeyOverride(resultAttr!, prop.Name);

            if (seenResultKeys.ContainsKey(resultKey))
            {
                continue;
            }

            seenResultKeys[resultKey] = prop.Name;

            resultProperties.Add(new ToolResultPropertyInfo(
                propertyName: prop.Name,
                resultKey: resultKey,
                typeName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                isNullable: IsNullable(prop.Type),
                typeKind: GetTypeKind(prop.Type)));
        }

        return resultProperties;
    }

    private static bool HasAttribute(IPropertySymbol prop, string attributeFullName, out AttributeData? attribute)
    {
        foreach (var attr in prop.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == attributeFullName)
            {
                attribute = attr;
                return true;
            }
        }

        attribute = null;
        return false;
    }

    private static string GetKeyOverride(AttributeData attribute, string defaultKey)
    {
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == "Name" && namedArg.Value.Value is string nameOverride && !string.IsNullOrEmpty(nameOverride))
            {
                return nameOverride;
            }
        }

        return defaultKey;
    }

    private static bool IsNullable(ITypeSymbol type)
        => type.NullableAnnotation == NullableAnnotation.Annotated
            || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

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
