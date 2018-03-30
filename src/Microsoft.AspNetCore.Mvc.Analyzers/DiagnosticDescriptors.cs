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
    }
}
