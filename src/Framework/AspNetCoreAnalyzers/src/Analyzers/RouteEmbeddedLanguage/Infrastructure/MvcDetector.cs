// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class MvcDetector
{
    private const string ControllerTypeNameSuffix = "Controller";

    // Replicates logic from ControllerFeatureProvider.IsController.
    // https://github.com/dotnet/aspnetcore/blob/785cf9bd845a8d28dce3a079c4fedf4a4c2afe57/src/Mvc/Mvc.Core/src/Controllers/ControllerFeatureProvider.cs#L39
    public static bool IsController(INamedTypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (!typeSymbol.IsReferenceType)
        {
            return false;
        }

        if (typeSymbol.IsAbstract)
        {
            return false;
        }

        // We only consider public top-level classes as controllers.
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }
        if (typeSymbol.ContainingType != null)
        {
            return false;
        }

        // Has generic arguments
        if (typeSymbol.IsGenericType)
        {
            return false;
        }

        // Check name before attribute's for performance.
        if (!typeSymbol.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
            !typeSymbol.HasAttribute(wellKnownTypes.ControllerAttribute))
        {
            return false;
        }

        if (typeSymbol.HasAttribute(wellKnownTypes.NonControllerAttribute))
        {
            return false;
        }

        return true;
    }

    // Replicates logic from DefaultApplicationModelProvider.IsAction.
    // https://github.com/dotnet/aspnetcore/blob/785cf9bd845a8d28dce3a079c4fedf4a4c2afe57/src/Mvc/Mvc.Core/src/ApplicationModels/DefaultApplicationModelProvider.cs#L393
    public static bool IsAction(IMethodSymbol methodSymbol, WellKnownTypes wellKnownTypes)
    {
        if (methodSymbol == null)
        {
            throw new ArgumentNullException(nameof(methodSymbol));
        }

        // The SpecialName bit is set to flag members that are treated in a special way by some compilers
        // (such as property accessors and operator overloading methods).
        if (methodSymbol.MethodKind is not (MethodKind.Ordinary or MethodKind.DeclareMethod))
        {
            return false;
        }

        // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
        if (methodSymbol.ContainingType.SpecialType is SpecialType.System_Object)
        {
            return false;
        }

        if (methodSymbol.IsStatic)
        {
            return false;
        }

        if (methodSymbol.IsAbstract)
        {
            return false;
        }

        if (methodSymbol.IsGenericMethod)
        {
            return false;
        }

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (methodSymbol.HasAttribute(wellKnownTypes.NonActionAttribute))
        {
            return false;
        }

        return true;
    }
}
