// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis;

internal static class CodeAnalysisExtensions
{
    public static bool HasAttribute(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        => GetAttributes(typeSymbol, attribute, inherit).Any();

    public static bool HasAttribute(this IMethodSymbol methodSymbol, ITypeSymbol attribute, bool inherit)
        => GetAttributes(methodSymbol, attribute, inherit).Any();

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attribute)
    {
        foreach (var declaredAttribute in symbol.GetAttributes())
        {
            if (declaredAttribute.AttributeClass is not null && attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
            {
                yield return declaredAttribute;
            }
        }
    }

    public static IEnumerable<AttributeData> GetAttributes(this IMethodSymbol methodSymbol, ITypeSymbol attribute, bool inherit)
    {
        Debug.Assert(methodSymbol != null);
        attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        IMethodSymbol? current = methodSymbol;
        while (current != null)
        {
            foreach (var attributeData in GetAttributes(current, attribute))
            {
                yield return attributeData;
            }

            if (!inherit)
            {
                break;
            }

            current = current.IsOverride ? current.OverriddenMethod : null;
        }
    }

    public static IEnumerable<AttributeData> GetAttributes(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
    {
        typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
        attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        foreach (var type in GetTypeHierarchy(typeSymbol))
        {
            foreach (var attributeData in GetAttributes(type, attribute))
            {
                yield return attributeData;
            }

            if (!inherit)
            {
                break;
            }
        }
    }

    public static bool HasAttribute(this IPropertySymbol propertySymbol, ITypeSymbol attribute, bool inherit)
    {
        propertySymbol = propertySymbol ?? throw new ArgumentNullException(nameof(propertySymbol));
        attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        if (!inherit)
        {
            return HasAttribute(propertySymbol, attribute);
        }

        IPropertySymbol? current = propertySymbol;
        while (current != null)
        {
            if (current.HasAttribute(attribute))
            {
                return true;
            }

            current = current.IsOverride ? current.OverriddenProperty : null;
        }

        return false;
    }

    public static bool IsAssignableFrom(this ITypeSymbol source, ITypeSymbol target)
    {
        source = source ?? throw new ArgumentNullException(nameof(source));
        target = target ?? throw new ArgumentNullException(nameof(target));

        if (SymbolEqualityComparer.Default.Equals(source, target))
        {
            return true;
        }

        if (source.TypeKind == TypeKind.Interface)
        {
            foreach (var @interface in target.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(source, @interface))
                {
                    return true;
                }
            }

            return false;
        }

        foreach (var type in target.GetTypeHierarchy())
        {
            if (SymbolEqualityComparer.Default.Equals(source, type))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attribute)
    {
        foreach (var declaredAttribute in symbol.GetAttributes())
        {
            if (declaredAttribute.AttributeClass is not null && attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol? typeSymbol)
    {
        while (typeSymbol != null)
        {
            yield return typeSymbol;

            typeSymbol = typeSymbol.BaseType;
        }
    }

    // Adapted from https://github.com/dotnet/roslyn/blob/929272/src/Workspaces/Core/Portable/Shared/Extensions/IMethodSymbolExtensions.cs#L61
    public static IEnumerable<IMethodSymbol> GetAllMethodSymbolsOfPartialParts(this IMethodSymbol method)
    {
        if (method.PartialDefinitionPart != null)
        {
            Debug.Assert(method.PartialImplementationPart == null && !SymbolEqualityComparer.Default.Equals(method.PartialDefinitionPart, method));
            yield return method;
            yield return method.PartialDefinitionPart;
        }
        else if (method.PartialImplementationPart != null)
        {
            Debug.Assert(!SymbolEqualityComparer.Default.Equals(method.PartialImplementationPart, method));
            yield return method.PartialImplementationPart;
            yield return method;
        }
        else
        {
            yield return method;
        }
    }

    // Adapted from IOperationExtensions.GetReceiverType in dotnet/roslyn-analyzers.
    // See https://github.com/dotnet/roslyn-analyzers/blob/762b08948cdcc1d94352fba681296be7bf474dd7/src/Utilities/Compiler/Extensions/IOperationExtensions.cs#L22-L51
    public static INamedTypeSymbol? GetReceiverType(
        this IInvocationOperation invocation,
        CancellationToken cancellationToken)
    {
        if (invocation.Instance != null)
        {
            return GetReceiverType(invocation.Instance.Syntax, invocation.SemanticModel, cancellationToken);
        }
        else if (invocation.TargetMethod.IsExtensionMethod && !invocation.TargetMethod.Parameters.IsEmpty)
        {
            var firstArg = invocation.Arguments.FirstOrDefault();
            if (firstArg != null)
            {
                return GetReceiverType(firstArg.Value.Syntax, invocation.SemanticModel, cancellationToken);
            }
            else if (invocation.TargetMethod.Parameters[0].IsParams)
            {
                return invocation.TargetMethod.Parameters[0].Type as INamedTypeSymbol;
            }
        }

        return null;

        static INamedTypeSymbol? GetReceiverType(
            SyntaxNode receiverSyntax,
            SemanticModel? model,
            CancellationToken cancellationToken)
        {
            var typeInfo = model?.GetTypeInfo(receiverSyntax, cancellationToken);
            return typeInfo?.Type as INamedTypeSymbol;
        }
    }
}
