// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsController(ITypeSymbol? classSymbol, WellKnownTypes wellKnownTypes)
    {
        // A controller is an instantiable class, usually public, in which at least one of the following conditions is true:
        //     - The class name is suffixed with Controller.
        //     - The class inherits from a class whose name is suffixed with Controller.
        //     - The [Controller] attribute is applied to the class.
        //     - - [ApiController] is possible value for ASP.MVC

        var isControllerInName = classSymbol?.Name.EndsWith("Controller", StringComparison.Ordinal) ?? false;

        return isControllerInName || IsInheritFromController(classSymbol?.BaseType, wellKnownTypes) || HasControllerAttribute(classSymbol, wellKnownTypes);
    }

    private static bool HasControllerAttribute(ITypeSymbol? classSymbol, WellKnownTypes wellKnownTypes)
    {
        return classSymbol != null && classSymbol.HasAttribute(wellKnownTypes.ControllerAttribute) || classSymbol!.HasAttribute(wellKnownTypes.ApiControllerAttribute);
    }

    private static bool IsInheritFromController(INamedTypeSymbol? typeSymbol, WellKnownTypes wellKnownTypes)
    {
        const string ControllerNameSuffix = "Controller";
        var lookupType = wellKnownTypes.ControllerBaseInstance;

        return typeSymbol != null && lookupType.IsAssignableFrom(typeSymbol) || typeSymbol!.Name.EndsWith(ControllerNameSuffix, StringComparison.Ordinal);
    }
}
