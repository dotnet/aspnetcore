// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

internal static class SymbolHelpers
{
    public static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat;

    // FullyQualifiedFormat drops nullable reference type annotations, which is what `typeof(...)`
    // needs (`typeof(string?)` does not compile) but is wrong for a cast: casting an object to
    // `EventCallback<string>` when the property is declared `EventCallback<string?>` is a nullability
    // mismatch (CS8619). Casts and generated accessor signatures use this format instead.
    public static readonly SymbolDisplayFormat FullyQualifiedWithNullability =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static string FullName(this ITypeSymbol type) => type.ToDisplayString(FullyQualified);

    // The declared type including nullable reference type annotations, for use in casts and in the
    // signatures of generated accessors, where the annotations have to match the declaration.
    public static string AnnotatedFullName(this ITypeSymbol type) =>
        type.ToDisplayString(FullyQualifiedWithNullability);

    // Walks the compilation's own types plus the types of every referenced assembly that uses
    // Components. Framework-owned components and JS-invokable methods require generated metadata too
    // when their reflection fallbacks are disabled.
    public static IEnumerable<INamedTypeSymbol> EnumerateApplicationTypes(
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace, cancellationToken))
        {
            yield return type;
        }

        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsComponentsAssembly(reference) && !ReferencesComponents(reference))
            {
                continue;
            }

            foreach (var type in EnumerateTypes(reference.GlobalNamespace, cancellationToken))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol @namespace, CancellationToken cancellationToken)
    {
        foreach (var member in @namespace.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (member)
            {
                case INamespaceSymbol nested:
                    foreach (var type in EnumerateTypes(nested, cancellationToken))
                    {
                        yield return type;
                    }

                    break;

                case INamedTypeSymbol type:
                    foreach (var nested in EnumerateNestedTypes(type))
                    {
                        yield return nested;
                    }

                    break;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var deeper in EnumerateNestedTypes(nested))
            {
                yield return deeper;
            }
        }
    }

    public static bool ReferencesComponents(IAssemblySymbol assembly)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var referenced in module.ReferencedAssemblies)
            {
                if (string.Equals(referenced.Name, WellKnownTypes.ComponentsAssemblyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsComponentsAssembly(IAssemblySymbol assembly)
        => string.Equals(
            assembly.Identity.Name,
            WellKnownTypes.ComponentsAssemblyName,
            StringComparison.Ordinal);

    public static bool IsFrameworkAssembly(IAssemblySymbol assembly)
    {
        var name = assembly.Identity.Name;
        return name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
               name.StartsWith("System.", StringComparison.Ordinal) ||
               string.Equals(name, "System", StringComparison.Ordinal) ||
               string.Equals(name, "netstandard", StringComparison.Ordinal) ||
               string.Equals(name, "mscorlib", StringComparison.Ordinal) ||
               string.Equals(name, "WindowsBase", StringComparison.Ordinal);
    }

    // Accessible from generated code that lives in the application assembly, which is what every
    // emitted lambda needs: the type and all of its containers have to be public.
    public static bool IsPubliclyAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsOrInheritsFrom(INamedTypeSymbol? candidate, INamedTypeSymbol? target)
    {
        if (candidate is null || target is null)
        {
            return false;
        }

        for (var current = candidate; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility is Accessibility.Public)
            {
                if (!HasRequiredMembers(type) || HasSetsRequiredMembersAttribute(constructor))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasRequiredMembers(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSetsRequiredMembersAttribute(IMethodSymbol constructor)
    {
        foreach (var attribute in constructor.GetAttributes())
        {
            if (string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
        {
            return null;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                return attribute;
            }
        }

        return null;
    }

    public static AttributeData? FindAttributeDerivedFrom(ISymbol symbol, INamedTypeSymbol? attributeBase)
    {
        if (attributeBase is null)
        {
            return null;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsOrInheritsFrom(attribute.AttributeClass, attributeBase))
            {
                return attribute;
            }
        }

        return null;
    }

    public static string ToStringLiteral(string value)
        => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
