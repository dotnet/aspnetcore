// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class ApiDiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor API1000_ActionReturnsUndocumentedStatusCode =
        new DiagnosticDescriptor(
            "API1000",
            "Action returns undeclared status code",
            "Action method returns undeclared status code '{0}'",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor API1001_ActionReturnsUndocumentedSuccessResult =
        new DiagnosticDescriptor(
            "API1001",
            "Action returns undeclared success result",
            "Action method returns a success result without a corresponding ProducesResponseType",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor API1002_ActionDoesNotReturnDocumentedStatusCode =
        new DiagnosticDescriptor(
            "API1002",
            "Action documents status code that is not returned",
            "Action method documents status code '{0}' without a corresponding return type",
            "Usage",
            DiagnosticSeverity.Info,
            isEnabledByDefault: false);

    public static readonly DiagnosticDescriptor API1003_ApiActionsDoNotRequireExplicitModelValidationCheck =
        new DiagnosticDescriptor(
            "API1003",
            "Action methods on ApiController instances do not require explicit model validation check",
            "Action methods on ApiController instances do not require explicit model validation check",
            "Usage",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            customTags: new[] { WellKnownDiagnosticTags.Unnecessary });
}
