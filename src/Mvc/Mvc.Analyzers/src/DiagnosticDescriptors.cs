// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MVC1000_HtmlHelperPartialShouldBeAvoided =
        new DiagnosticDescriptor(
            "MVC1000",
            "Use of IHtmlHelper.{0} should be avoided",
            "Use of IHtmlHelper.{0} may result in application deadlocks. Consider using <partial> Tag Helper or IHtmlHelper.{0}Async.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods =
        new DiagnosticDescriptor(
            "MVC1001",
            "Filters cannot be applied to page handler methods",
            "'{0}' cannot be applied to Razor Page handler methods. It may be applied either to the Razor Page model or applied globally.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods =
        new DiagnosticDescriptor(
            "MVC1002",
            "Route attributes cannot be applied to page handler methods",
            "'{0}' cannot be applied to Razor Page handler methods. Routes for Razor Pages must be declared using the @page directive or using conventions.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MVC1003_RouteAttributesShouldNotBeAppliedToPageModels =
        new DiagnosticDescriptor(
            "MVC1003",
            "Route attributes cannot be applied to page models",
            "'{0}' cannot be applied to a Razor Page model. Routes for Razor Pages must be declared using the @page directive or using conventions.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MVC1004_ParameterNameCollidesWithTopLevelProperty =
        new DiagnosticDescriptor(
            "MVC1004",
            "Rename model bound parameter",
            "Property on type '{0}' has the same name as parameter '{1}'. This may result in incorrect model binding. " +
            "Consider renaming the parameter or the property to avoid conflicts. If the type '{0}' has a custom type converter or custom model binder, you can suppress this message.",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/AA20pbc");

    // MVC1005 reserved for startup

    public static readonly DiagnosticDescriptor MVC1006_FunctionsContainingTagHelpersMustBeAsyncAndReturnTask =
        new DiagnosticDescriptor(
            "MVC1006",
            "Methods containing TagHelpers must be async and return Task",
            "The method contains a TagHelper and therefore must be async and return a Task. For instance, usage of ~/ typically results in a TagHelper and requires an async Task returning parent method.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
