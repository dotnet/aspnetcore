// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectMismatchedParameterOptionality(
        in OperationAnalysisContext context,
        RouteUsageModel routeUsage,
        IMethodSymbol methodSymbol)
    {
        var allDeclarations = methodSymbol.GetAllMethodSymbolsOfPartialParts();
        foreach (var method in allDeclarations)
        {
            foreach (var parameter in method.Parameters)
            {
                var paramName = parameter.Name;

                //  If this is not the methpd parameter associated with the route
                // parameter then continue looking for it in the list
                if (!routeUsage.RoutePattern.TryGetRouteParameter(paramName, out var routeParameter))
                {
                    continue;
                }

                var argumentIsOptional = parameter.IsOptional || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated;
                if (!argumentIsOptional && routeParameter.IsOptional)
                {
                    var location = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DetectMismatchedParameterOptionality,
                        location,
                        paramName));
                }
            }
        }
    }
}
