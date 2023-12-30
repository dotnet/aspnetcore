// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowMvcBindArgumentsOnParameters(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IInvocationOperation invocation,
        IMethodSymbol methodSymbol)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            var modelBindingAttribute = parameter.GetAttributes(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_ModelBinding_IBinderTypeProviderMetadata)).FirstOrDefault() ??
                parameter.GetAttributes(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_BindAttribute)).FirstOrDefault();

            if (modelBindingAttribute?.AttributeClass is not null)
            {
                var location = Location.None;
                if (!parameter.DeclaringSyntaxReferences.IsEmpty)
                {
                    var syntax = parameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                    location = syntax.GetLocation();
                }

                var methodName = invocation.TargetMethod.Name;

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters,
                    location,
                    modelBindingAttribute.AttributeClass.Name,
                    methodName));
            }
        }
    }
}
