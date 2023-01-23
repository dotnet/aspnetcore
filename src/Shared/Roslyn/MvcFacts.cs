// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Shared;

internal static class MvcFacts
{
    public static bool IsController(INamedTypeSymbol type, INamedTypeSymbol controllerAttribute, INamedTypeSymbol nonControllerAttribute)
    {
        type = type ?? throw new ArgumentNullException(nameof(type));
        controllerAttribute = controllerAttribute ?? throw new ArgumentNullException(nameof(controllerAttribute));
        nonControllerAttribute = nonControllerAttribute ?? throw new ArgumentNullException(nameof(nonControllerAttribute));

        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.IsAbstract)
        {
            return false;
        }

        // We only consider public top-level classes as controllers.
        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (type.ContainingType != null)
        {
            return false;
        }

        if (type.IsGenericType || type.IsUnboundGenericType)
        {
            return false;
        }

        if (type.HasAttribute(nonControllerAttribute, inherit: true))
        {
            return false;
        }

        if (!type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) &&
            !type.HasAttribute(controllerAttribute, inherit: true))
        {
            return false;
        }

        return true;
    }

    public static bool IsControllerAction(IMethodSymbol method, INamedTypeSymbol nonActionAttribute, IMethodSymbol disposableDispose)
    {
        method = method ?? throw new ArgumentNullException(nameof(method));
        nonActionAttribute = nonActionAttribute ?? throw new ArgumentNullException(nameof(nonActionAttribute));

        if (method.MethodKind != MethodKind.Ordinary)
        {
            return false;
        }

        if (method.HasAttribute(nonActionAttribute, inherit: true))
        {
            return false;
        }

        // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
        if (GetDeclaringType(method).SpecialType == SpecialType.System_Object)
        {
            return false;
        }

        if (IsIDisposableDispose(method, disposableDispose))
        {
            return false;
        }

        if (method.IsStatic)
        {
            return false;
        }

        if (method.IsAbstract)
        {
            return false;
        }

        if (method.IsGenericMethod)
        {
            return false;
        }

        return method.DeclaredAccessibility == Accessibility.Public;
    }

    private static INamedTypeSymbol GetDeclaringType(IMethodSymbol method)
    {
        while (method.IsOverride)
        {
            if (method.OverriddenMethod == null)
            {
                throw new InvalidOperationException($"{nameof(method.OverriddenMethod)} cannot be null.");
            }

            method = method.OverriddenMethod;
        }

        return method.ContainingType;
    }

    private static bool IsIDisposableDispose(IMethodSymbol method, IMethodSymbol disposableDispose)
    {
        if (method.Name != disposableDispose.Name)
        {
            return false;
        }

        if (!method.ReturnsVoid)
        {
            return false;
        }

        if (method.Parameters.Length != disposableDispose.Parameters.Length)
        {
            return false;
        }

        // Explicit implementation
        for (var i = 0; i < method.ExplicitInterfaceImplementations.Length; i++)
        {
            if (method.ExplicitInterfaceImplementations[i].ContainingType.SpecialType == SpecialType.System_IDisposable)
            {
                return true;
            }
        }

        var implementedMethod = method.ContainingType.FindImplementationForInterfaceMember(disposableDispose);
        return SymbolEqualityComparer.Default.Equals(implementedMethod, method);
    }
}
