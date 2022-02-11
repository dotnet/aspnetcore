// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

// RazorPage is usually must be inherited from PageModel and Handler starts with On + HttpMethodName

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsRazorPage(ITypeSymbol? classSymbol, WellKnownTypes wellKnownTypes)
    {
        // RazorPage must be inherited from the Microsoft.AspNetCore.Mvc.RazorPages.PageModel
        INamedTypeSymbol lookupType = wellKnownTypes.PageModel;

        return classSymbol?.BaseType != null && lookupType.IsAssignableFrom(classSymbol?.BaseType!);
    }

    private static bool IsRazorPageHandlerMethod(IMethodSymbol? methodSymbol, WellKnownTypes wellKnownTypes)
    {
        // if method is marked by [NonHandler] don't process it disregarding its' name
        if (methodSymbol?.HasAttribute(wellKnownTypes.NonHandlerAttribute) ?? false)
        {
            return false;
        }

        var methodName = methodSymbol?.Name ?? string.Empty;

        // Check if method name follows On + HttpRequestMethod (currently GET, POST, PUT, DELETE are considered)
        var possibleMethodNames = ImmutableArray.Create("onget", "onput", "onpost", "ondelete");
        bool isMatch = false;
        for (int i = 0; i < possibleMethodNames.Length; i++)
        {
            isMatch |= methodName.StartsWith(possibleMethodNames[i], ignoreCase: true, CultureInfo.InvariantCulture);
            if (isMatch)
            {
                break;
            }
        }

        return isMatch;
    }
}
