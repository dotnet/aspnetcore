// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void AtMostOneFromBodyAttribute(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IMethodSymbol methodSymbol)
    {
        var fromBodyMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata);
        var asParametersAttributeType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute);

        var asParametersDecoratedParameters = methodSymbol.Parameters.Where(p => p.HasAttribute(asParametersAttributeType));

        foreach (var asParameterDecoratedParameter in asParametersDecoratedParameters)
        {
            var fromBodyMetadataInterfaceMembers = asParameterDecoratedParameter.Type.GetMembers().Where(
                m => m.HasAttributeImplementingInterface(fromBodyMetadataInterfaceType)
                );

            if (fromBodyMetadataInterfaceMembers.Count() >= 2)
            {
                ReportDiagnostics(context, fromBodyMetadataInterfaceMembers);
            }
        }

        var fromBodyMetadataInterfaceParameters = methodSymbol.Parameters.Where(p => p.HasAttributeImplementingInterface(fromBodyMetadataInterfaceType));

        if (fromBodyMetadataInterfaceParameters.Count() >= 2)
        {
            ReportDiagnostics(context, fromBodyMetadataInterfaceParameters);
        }

        static void ReportDiagnostics(OperationAnalysisContext context, IEnumerable<ISymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.DeclaringSyntaxReferences.Length > 0)
                {
                    var syntax = symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                    var location = syntax.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AtMostOneFromBodyAttribute,
                        location
                        ));
                }
            }
        }
    }
}
