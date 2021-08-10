// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.MinimalActions;

public partial class MinimalActionAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowMvcBindArgumentsOnParameters(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IMethodSymbol methodSymbol)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            var modelBindingAttribute = parameter.GetAttributes(wellKnownTypes.IBinderTypeProviderMetadata).FirstOrDefault() ??
                parameter.GetAttributes(wellKnownTypes.BindAttribute).FirstOrDefault();

            if (modelBindingAttribute is not null)
            {
                var location = Location.None;
                if (!parameter.DeclaringSyntaxReferences.IsEmpty)
                {
                    var syntax = parameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                    location = syntax.GetLocation();
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DoNotUseModelBindingAttributesOnMinimalActionParameters,
                    location,
                    modelBindingAttribute.AttributeClass.Name));
            }
        }
    }
}
