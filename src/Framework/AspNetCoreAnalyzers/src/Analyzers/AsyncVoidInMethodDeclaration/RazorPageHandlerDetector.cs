// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        // Check if method name follows On + HttpRequestMethod (currently GET, POST, PUT, DELETE are considered)
        const string OnGet = "onget";
        const string OnPut = "onput";
        const string OnPost = "onpost";
        const string OnDelete = "ondelete";

        if (methodSymbol?.HasAttribute(wellKnownTypes.NonHandlerAttribute) ?? false)
        {
            // if method is marked by [NonHandler] don't process it disregarding its' name
            return false;
        }

        var methodName = methodSymbol?.Name ?? string.Empty;

        return methodName.StartsWith(OnGet, ignoreCase: true, CultureInfo.InvariantCulture)
            || methodName.StartsWith(OnPost, ignoreCase: true, CultureInfo.InvariantCulture)
            || methodName.StartsWith(OnPut, ignoreCase: true, CultureInfo.InvariantCulture)
            || methodName.StartsWith(OnDelete, ignoreCase: true, CultureInfo.InvariantCulture);
    }
}
