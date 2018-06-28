// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor MVC1000_HtmlHelperPartialShouldBeAvoided =
            new DiagnosticDescriptor(
                "MVC1000",
                "Use of IHtmlHelper.{0} should be avoided.",
                "Use of IHtmlHelper.{0} may result in application deadlocks. Consider using <partial> Tag Helper or IHtmlHelper.{0}Async.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods =
            new DiagnosticDescriptor(
                "MVC1001",
                "Filters cannot be applied to page handler methods.",
                "'{0}' cannot be applied to Razor Page handler methods. It may be applied either to the Razor Page model or applied globally.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods =
            new DiagnosticDescriptor(
                "MVC1002",
                "Route attributes cannot be applied to page handler methods.",
                "'{0}' cannot be applied to Razor Page handler methods. Routes for Razor Pages must be declared using the @page directive or using conventions.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1003_RouteAttributesShouldNotBeAppliedToPageModels =
            new DiagnosticDescriptor(
                "MVC1003",
                "Route attributes cannot be applied to page models.",
                "'{0}' cannot be applied to a Razor Page model. Routes for Razor Pages must be declared using the @page directive or using conventions.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1004_ActionReturnsUndocumentedStatusCode =
            new DiagnosticDescriptor(
                "MVC1004",
                "Action returns undeclared status code.",
                "Action method returns undeclared status code '{0}'.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1005_ActionReturnsUndocumentedSuccessResult =
            new DiagnosticDescriptor(
                "MVC1005",
                "Action returns undeclared success result.",
                "Action method returns a success result without a corresponding ProducesResponseType.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MVC1006_ActionDoesNotReturnDocumentedStatusCode =
            new DiagnosticDescriptor(
                "MVC1006",
                "Action documents status code that is not returned.",
                "Action method documents status code '{0}' without a corresponding return type.",
                "Usage",
                DiagnosticSeverity.Info,
                isEnabledByDefault: false);
    }
}
