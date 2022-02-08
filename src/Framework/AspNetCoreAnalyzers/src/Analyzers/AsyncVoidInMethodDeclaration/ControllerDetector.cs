// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsController(ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context, WellKnownTypes wellKnownTypes)
    {
        // A controller is an instantiable class, usually public, in which at least one of the following conditions is true:
        //     - The class name is suffixed with Controller.
        //     - The class inherits from a class whose name is suffixed with Controller.
        //     - The [Controller] attribute is applied to the class.
        //     - - [ApiController] is possible value for ASP.MVC

        var classSymbol = (ITypeSymbol?)context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        var isControllerInName = classSymbol?.Name.EndsWith("Controller", StringComparison.Ordinal) ?? false;

        return isControllerInName || IsInheritFromController(classSymbol?.BaseType, wellKnownTypes) || HasControllerAttribute(classDeclaration);
    }

    private static bool HasControllerAttribute(ClassDeclarationSyntax classDeclaration)
    {
        const string NetCoreControllerAttribute = "Controller";
        const string AspMvcAdditionalControllerAttribute = "ApiController";

        for (int i = 0; i < classDeclaration.AttributeLists.Count; i++)
        {
            var attributeListsValue = classDeclaration.AttributeLists[i].ToString();
            if (attributeListsValue.Contains(NetCoreControllerAttribute) || attributeListsValue.Contains(AspMvcAdditionalControllerAttribute))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInheritFromController(INamedTypeSymbol? typeSymbol, WellKnownTypes wellKnownTypes)
    {
        const string ControllerSignSuffix = "Controller";

        // check if a class inherits from Microsoft.AspNetCore.Mvc.ControllerBase or Microsoft.AspNetCore.Mvc.Controller
        ImmutableArray<INamedTypeSymbol> lookupTypes = ImmutableArray.Create(wellKnownTypes.ControllerInstance, wellKnownTypes.ControllerBaseInstance);
        for (int i = 0; i < lookupTypes.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(lookupTypes[i], typeSymbol))
            {
                return true;
            }
        }

        // a base class could be a custom controller, so that check base class suffix
        return typeSymbol?.Name.EndsWith(ControllerSignSuffix, StringComparison.Ordinal) ?? false;
    }
}
