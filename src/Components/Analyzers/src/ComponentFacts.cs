// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Analyzers;

internal static class ComponentFacts
{
    public static bool IsAnyParameter(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return property.GetAttributes().Any(a =>
        {
            return SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.ParameterAttribute) || SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.CascadingParameterAttribute);
        });
    }

    public static bool IsParameter(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.ParameterAttribute));
    }

    public static bool IsParameterWithCaptureUnmatchedValues(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        var attribute = property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.ParameterAttribute));
        if (attribute == null)
        {
            return false;
        }

        foreach (var kvp in attribute.NamedArguments)
        {
            if (string.Equals(kvp.Key, ComponentsApi.ParameterAttribute.CaptureUnmatchedValues, StringComparison.Ordinal))
            {
                return kvp.Value.Value as bool? ?? false;
            }
        }

        return false;
    }

    public static bool IsCascadingParameter(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.CascadingParameterAttribute));
    }

    public static bool IsSupplyParameterFromForm(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (symbols.SupplyParameterFromFormAttribute == null)
        {
            return false;
        }

        return property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.SupplyParameterFromFormAttribute));
    }

    public static bool IsPersistentState(ComponentSymbols symbols, IPropertySymbol property)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (symbols.PersistentStateAttribute == null)
        {
            return false;
        }

        return property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.PersistentStateAttribute));
    }

    public static bool IsComponentBase(ComponentSymbols symbols, INamedTypeSymbol type)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (symbols.ComponentBaseType == null)
        {
            return false;
        }

        // Check if the type inherits from ComponentBase
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, symbols.ComponentBaseType))
            {
                return true;
            }
            current = current.BaseType;
        }

        return false;
    }

    public static bool IsComponent(ComponentSymbols symbols, Compilation compilation, INamedTypeSymbol type)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var conversion = compilation.ClassifyConversion(symbols.IComponentType, type);
        if (!conversion.Exists || !conversion.IsExplicit)
        {
            return false;
        }

        return true;
    }

    public static bool IsOpenComponentInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText != "OpenComponent")
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return method.ContainingType?.Name == "RenderTreeBuilder" &&
                   method.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering";
        }

        return false;
    }

    public static bool IsCloseComponentInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText != "CloseComponent")
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return method.ContainingType?.Name == "RenderTreeBuilder" &&
                   method.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering";
        }

        return false;
    }

    public static bool IsRenderTreeBuilderMethodInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string methodName)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText != methodName)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return method.ContainingType?.Name == "RenderTreeBuilder" &&
                   method.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering";
        }

        return false;
    }
}
