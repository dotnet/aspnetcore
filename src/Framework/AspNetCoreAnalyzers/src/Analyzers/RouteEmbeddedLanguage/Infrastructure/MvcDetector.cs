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
    public static bool IsController(ITypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        if (!typeSymbol.IsReferenceType)
        {
            return false;
        }

        if (typeSymbol.IsAbstract)
        {
            return false;
        }

        // We only consider public top-level classes as controllers. IsPublic returns false for nested
        // classes, regardless of visibility modifiers
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        // Has generic arguments
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
        {
            return false;
        }

        if (typeSymbol.HasAttribute("Microsoft.AspNetCore.Mvc.NonControllerAttribute", semanticModel))
        {
            return false;
        }

        if (!typeSymbol.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
            !typeSymbol.HasAttribute("Microsoft.AspNetCore.Mvc.ControllerAttribute", semanticModel))
        {
            return false;
        }

        return true;
    }

    // Replicates logic from DefaultApplicationModelProvider.IsAction.
    // https://github.com/dotnet/aspnetcore/blob/785cf9bd845a8d28dce3a079c4fedf4a4c2afe57/src/Mvc/Mvc.Core/src/ApplicationModels/DefaultApplicationModelProvider.cs#L393
    public static bool IsAction(IMethodSymbol methodSymbol, SemanticModel semanticModel)
    {
        if (methodSymbol == null)
        {
            throw new ArgumentNullException(nameof(methodSymbol));
        }

        // The SpecialName bit is set to flag members that are treated in a special way by some compilers
        // (such as property accessors and operator overloading methods).
        if (methodSymbol.MethodKind is not MethodKind.Ordinary or MethodKind.DeclareMethod)
        {
            return false;
        }

        if (methodSymbol.HasAttribute("Microsoft.AspNetCore.Mvc.NonActionAttribute", semanticModel))
        {
            return false;
        }

        // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
        if (methodSymbol.OriginalDefinition.ContainingType is
            {
                Name: "Object",
                ContainingNamespace:
                {
                    Name: "System",
                    IsGlobalNamespace: true
                }
            })
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

        return methodSymbol.DeclaredAccessibility == Accessibility.Public;
    }
}
