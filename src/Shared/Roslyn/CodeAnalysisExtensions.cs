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
    internal enum SymbolVisibility
    {
        Public,
        Internal,
        Private,
    }

    // Copy from https://github.com/dotnet/roslyn/blob/d2ff1d83e8fde6165531ad83f0e5b1ae95908289/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs#L28-L73
    public static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
    {
        // Start by assuming it's visible.
        var visibility = SymbolVisibility.Public;
        switch (symbol.Kind)
        {
            case SymbolKind.Alias:
                // Aliases are uber private.  They're only visible in the same file that they
                // were declared in.
                return SymbolVisibility.Private;
            case SymbolKind.Parameter:
                // Parameters are only as visible as their containing symbol
                return GetResultantVisibility(symbol.ContainingSymbol);
            case SymbolKind.TypeParameter:
                // Type Parameters are private.
                return SymbolVisibility.Private;
        }
        while (symbol != null && symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                // If we see anything private, then the symbol is private.
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return SymbolVisibility.Private;
                // If we see anything internal, then knock it down from public to
                // internal.
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = SymbolVisibility.Internal;
                    break;
                    // For anything else (Public, Protected, ProtectedOrInternal), the
                    // symbol stays at the level we've gotten so far.
            }
            symbol = symbol.ContainingSymbol;
        }
        return visibility;
    }

    // Copy from https://github.com/dotnet/roslyn/blob/d2ff1d83e8fde6165531ad83f0e5b1ae95908289/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/CompilationExtensions.cs#L11-L68
    /// <summary>
    /// Gets a type by its metadata name to use for code analysis within a <see cref="Compilation"/>. This method
    /// attempts to find the "best" symbol to use for code analysis, which is the symbol matching the first of the
    /// following rules.
    ///
    /// <list type="number">
    ///   <item><description>
    ///     If only one type with the given name is found within the compilation and its referenced assemblies, that
    ///     type is returned regardless of accessibility.
    ///   </description></item>
    ///   <item><description>
    ///     If the current <paramref name="compilation"/> defines the symbol, that symbol is returned.
    ///   </description></item>
    ///   <item><description>
    ///     If exactly one referenced assembly defines the symbol in a manner that makes it visible to the current
    ///     <paramref name="compilation"/>, that symbol is returned.
    ///   </description></item>
    ///   <item><description>
    ///     Otherwise, this method returns <see langword="null"/>.
    ///   </description></item>
    /// </list>
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
    /// <param name="fullyQualifiedMetadataName">The fully-qualified metadata type name to find.</param>
    /// <returns>The symbol to use for code analysis; otherwise, <see langword="null"/>.</returns>
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
    {
        INamedTypeSymbol? type = null;

        foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
        {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
            {
                Debug.Assert(type is null);
                return currentType;
            }

            switch (currentType.GetResultantVisibility())
            {
                case SymbolVisibility.Public:
                case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                default:
                    continue;
            }

            if (type is object)
            {
                // Multiple visible types with the same metadata name are present
                return null;
            }

            type = currentType;
        }

        return type;
    }

    public static bool HasAttribute(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        => GetAttributes(typeSymbol, attribute, inherit).Any();

    public static bool HasAttribute(this IMethodSymbol methodSymbol, ITypeSymbol attribute, bool inherit)
        => GetAttributes(methodSymbol, attribute, inherit).Any();

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attribute)
    {
        foreach (var declaredAttribute in symbol.GetAttributes())
        {
            if (attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
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
            if (attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol? typeSymbol)
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
